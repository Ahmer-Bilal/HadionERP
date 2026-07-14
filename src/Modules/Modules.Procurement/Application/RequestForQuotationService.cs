using System.Text.Json;
using Modules.MasterData.Contracts;
using Modules.Procurement.Domain;
using Platform.Audit;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Procurement.Application;

public sealed class RequestForQuotationService
{
    public const string NumberRangeKey = "PROC-RFQ";

    /// <summary>Same commercial-relationship role set <c>Modules.Finance.Application.APInvoiceService</c>
    /// uses for <c>PayableEligibleRoles</c> — only a vendor actually able to supply/subcontract/consult can
    /// meaningfully be invited to quote a price.</summary>
    private static readonly HashSet<string> QuoteEligibleRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Supplier", "Subcontractor", "Consultant", "RentalCompany", "Manufacturer", "ManpowerSupplier", "TestingLaboratory",
    };

    private const string AuditTargetType = "RequestForQuotation";
    private const string AuditSource = "Modules.Procurement";

    private readonly IRequestForQuotationRepository _repository;
    private readonly IPurchaseRequisitionRepository _purchaseRequisitionRepository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IBusinessPartnerLookup _businessPartnerLookup;

    public RequestForQuotationService(
        IRequestForQuotationRepository repository,
        IPurchaseRequisitionRepository purchaseRequisitionRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IBusinessPartnerLookup businessPartnerLookup)
    {
        _repository = repository;
        _purchaseRequisitionRepository = purchaseRequisitionRepository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _businessPartnerLookup = businessPartnerLookup;
    }

    public async Task<RequestForQuotationDto> CreateAsync(
        CreateRequestForQuotationRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, RequestForQuotationSecurity.MaintainPrivilegeKey);

        if (request.InvitedVendorIds.Count == 0)
            throw new ArgumentException("A request for quotation needs at least one invited vendor.");

        var purchaseRequisition = await _purchaseRequisitionRepository.GetAsync(request.PurchaseRequisitionId, cancellationToken)
            ?? throw new ArgumentException($"Purchase requisition {request.PurchaseRequisitionId} was not found.");
        if (purchaseRequisition.Status != Platform.Core.BusinessObjectStatus.Approved)
            throw new ArgumentException($"Purchase requisition '{purchaseRequisition.DocumentNumber}' must be Approved before an RFQ can be raised against it.");

        foreach (var vendorId in request.InvitedVendorIds)
        {
            await ValidateVendorAsync(vendorId, cancellationToken);
        }

        var rfq = new RequestForQuotation(actor, request.PurchaseRequisitionId, request.Description, request.ResponseDeadline);
        foreach (var line in purchaseRequisition.Lines)
        {
            rfq.AddLine(line.Id, line.ItemId, line.Quantity);
        }
        foreach (var vendorId in request.InvitedVendorIds)
        {
            rfq.InviteVendor(vendorId);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        rfq.AssignNumber(documentNumber);

        _repository.Add(rfq);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(rfq.Id), actor,
            $"Request for quotation '{rfq.DocumentNumber}' created against '{purchaseRequisition.DocumentNumber}' ({request.InvitedVendorIds.Count} vendors invited).", AuditSource);

        return ToDto(rfq);
    }

    public async Task<RequestForQuotationDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rfq = await _repository.GetAsync(id, cancellationToken);
        return rfq is null ? null : ToDto(rfq);
    }

    public async Task<(IReadOnlyList<RequestForQuotationDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<RequestForQuotationDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, RequestForQuotationSecurity.MaintainPrivilegeKey);
        var rfq = await RequireRfqAsync(id, cancellationToken);

        var fromStatus = rfq.Status;
        rfq.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(rfq.Id), actor,
            $"Request for quotation '{rfq.DocumentNumber}' submitted (sent to invited vendors).",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(rfq.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(RequestForQuotationWorkflow.BusinessObjectType, RequestForQuotationWorkflow.SubmitTransition, rfq.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(rfq, actor, cancellationToken);
        }

        return ToDto(rfq);
    }

    public Task<RequestForQuotationDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<RequestForQuotationDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<RequestForQuotationDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, RequestForQuotationSecurity.ApprovePrivilegeKey);
        var rfq = await RequireRfqAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(RequestForQuotationWorkflow.BusinessObjectType, rfq.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Request for quotation {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(rfq, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(rfq, actor, cancellationToken);

        return ToDto(rfq);
    }

    public async Task<RequestForQuotationDto> RecordVendorQuoteAsync(
        Guid id, RecordVendorQuoteRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, RequestForQuotationSecurity.MaintainPrivilegeKey);
        var rfq = await RequireRfqAsync(id, cancellationToken);

        rfq.RecordVendorQuote(request.VendorId, request.RfqLineId, request.QuotedUnitPrice);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(rfq.Id), actor,
            $"Vendor quote recorded on request for quotation '{rfq.DocumentNumber}'.",
            new[] { new FieldValueChange("VendorQuoteLines", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.QuotedUnitPrice)) },
            AuditSource);

        return ToDto(rfq);
    }

    private async Task ApproveInternalAsync(RequestForQuotation rfq, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = rfq.Status;
        rfq.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(rfq.Id), actor,
            $"Request for quotation '{rfq.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(rfq.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(RequestForQuotation rfq, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = rfq.Status;
        rfq.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(rfq.Id), actor,
            $"Request for quotation '{rfq.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(rfq.Status.ToString()), AuditSource);
    }

    private async Task ValidateVendorAsync(Guid vendorId, CancellationToken cancellationToken)
    {
        var vendor = await _businessPartnerLookup.GetAsync(vendorId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {vendorId} was not found.");
        if (vendor.Status != "Approved")
            throw new ArgumentException($"Business partner '{vendor.Name}' is not Approved and cannot be invited to quote.");
        if (!vendor.BusinessRoles.Any(QuoteEligibleRoles.Contains))
            throw new ArgumentException($"Business partner '{vendor.Name}' does not hold a role eligible to quote.");
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static Platform.Core.BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<RequestForQuotation> RequireRfqAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Request for quotation {id} was not found.");

    private static RequestForQuotationDto ToDto(RequestForQuotation r) => new(
        r.Id, r.DocumentNumber, r.Status.ToString(), r.PurchaseRequisitionId, r.Description, r.ResponseDeadline,
        r.Lines.Select(l => new RfqLineDto(l.Id, l.PurchaseRequisitionLineId, l.ItemId, l.Quantity)).ToList(),
        r.InvitedVendors.Select(v => new RfqInvitedVendorDto(v.Id, v.VendorId)).ToList(),
        r.VendorQuoteLines.Select(q => new RfqVendorQuoteLineDto(q.Id, q.VendorId, q.RfqLineId, q.QuotedUnitPrice)).ToList(),
        r.CreatedAt, r.CreatedBy);
}
