using System.Text.Json;
using Modules.Finance.Contracts;
using Modules.MasterData.Contracts;
using Modules.Procurement.Domain;
using Platform.Audit;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Procurement.Application;

public sealed class PurchaseOrderService
{
    public const string NumberRangeKey = "PROC-PO";

    /// <summary>Same commercial-relationship role set <c>RequestForQuotationService.QuoteEligibleRoles</c>/
    /// <c>Modules.Finance.Application.APInvoiceService.PayableEligibleRoles</c> use — a PO is a commitment to
    /// pay, so only a vendor actually able to supply/subcontract/consult can receive one.</summary>
    private static readonly HashSet<string> VendorEligibleRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Supplier", "Subcontractor", "Consultant", "RentalCompany", "Manufacturer", "ManpowerSupplier", "TestingLaboratory",
    };

    private const string AuditTargetType = "PurchaseOrder";
    private const string AuditSource = "Modules.Procurement";

    private readonly IPurchaseOrderRepository _repository;
    private readonly IRequestForQuotationRepository _requestForQuotationRepository;
    private readonly IPurchaseRequisitionRepository _purchaseRequisitionRepository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IBusinessPartnerLookup _businessPartnerLookup;
    private readonly IItemLookup _itemLookup;
    private readonly ICostCenterLookup _costCenterLookup;
    private readonly IBudgetCheckService _budgetCheckService;

    public PurchaseOrderService(
        IPurchaseOrderRepository repository,
        IRequestForQuotationRepository requestForQuotationRepository,
        IPurchaseRequisitionRepository purchaseRequisitionRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IBusinessPartnerLookup businessPartnerLookup,
        IItemLookup itemLookup,
        ICostCenterLookup costCenterLookup,
        IBudgetCheckService budgetCheckService)
    {
        _repository = repository;
        _requestForQuotationRepository = requestForQuotationRepository;
        _purchaseRequisitionRepository = purchaseRequisitionRepository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _businessPartnerLookup = businessPartnerLookup;
        _itemLookup = itemLookup;
        _costCenterLookup = costCenterLookup;
        _budgetCheckService = budgetCheckService;
    }

    public async Task<PurchaseOrderDto> CreateAsync(
        CreatePurchaseOrderRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, PurchaseOrderSecurity.MaintainPrivilegeKey);
        await ValidateVendorAsync(request.VendorId, cancellationToken);

        var po = new PurchaseOrder(actor, request.VendorId, request.RequestForQuotationId);

        if (request.RequestForQuotationId is { } rfqId)
        {
            if (request.Lines is { Count: > 0 })
                throw new ArgumentException("A purchase order created from an RFQ cannot also carry direct lines.");
            await AddLinesFromRfqAsync(po, rfqId, request.VendorId, cancellationToken);
        }
        else
        {
            if (request.Lines is not { Count: > 0 })
                throw new ArgumentException("A direct purchase order needs at least one line.");
            foreach (var line in request.Lines)
            {
                await ValidateLineReferencesAsync(line.ItemId, line.CostCenterId, cancellationToken);
                po.AddLine(line.ItemId, line.CostCenterId, line.Quantity, line.UnitPrice);
            }
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        po.AssignNumber(documentNumber);

        _repository.Add(po);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(po.Id), actor,
            $"Purchase order '{po.DocumentNumber}' created ({po.Lines.Count} lines, total {po.Total}).", AuditSource);

        return ToDto(po);
    }

    public async Task<PurchaseOrderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _repository.GetAsync(id, cancellationToken);
        return po is null ? null : ToDto(po);
    }

    public async Task<(IReadOnlyList<PurchaseOrderDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    /// <summary>Runs the Finance budget check — one call per distinct cost center on the PO, summing that
    /// cost center's lines — before the PO is ever submitted for approval, matching
    /// docs/architecture/01-architecture-foundation.md §3.2's own example ("Procurement asks Finance's
    /// IBudgetCheckService before releasing a PO"). A denied cost center blocks the whole submit; nothing is
    /// partially submitted.</summary>
    public async Task<PurchaseOrderDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, PurchaseOrderSecurity.MaintainPrivilegeKey);
        var po = await RequirePurchaseOrderAsync(id, cancellationToken);

        foreach (var group in po.Lines.GroupBy(l => l.CostCenterId))
        {
            var amount = group.Sum(l => l.LineTotal);
            var result = await _budgetCheckService.CheckAsync(group.Key, amount, cancellationToken);
            if (!result.Allowed)
                throw new ArgumentException($"Budget check failed for cost center {group.Key}: {result.Reason}");
        }

        var fromStatus = po.Status;
        po.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(po.Id), actor,
            $"Purchase order '{po.DocumentNumber}' submitted for approval (budget check passed).",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(po.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(PurchaseOrderWorkflow.BusinessObjectType, PurchaseOrderWorkflow.SubmitTransition, po.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(po, actor, cancellationToken);
        }

        return ToDto(po);
    }

    public Task<PurchaseOrderDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<PurchaseOrderDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<PurchaseOrderDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, PurchaseOrderSecurity.ApprovePrivilegeKey);
        var po = await RequirePurchaseOrderAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(PurchaseOrderWorkflow.BusinessObjectType, po.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase order {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(po, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(po, actor, cancellationToken);

        return ToDto(po);
    }

    private async Task ApproveInternalAsync(PurchaseOrder po, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = po.Status;
        po.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(po.Id), actor,
            $"Purchase order '{po.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(po.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(PurchaseOrder po, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = po.Status;
        po.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(po.Id), actor,
            $"Purchase order '{po.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(po.Status.ToString()), AuditSource);
    }

    /// <summary>Builds the PO's lines from an Approved RFQ's own lines, at the price the given vendor
    /// quoted — the vendor must be one of the RFQ's invited vendors and must have quoted every one of its
    /// lines (a partial award, picking different vendors per line, is a future PO-splitting concern, not
    /// built here). Each line's Cost Center is traced back through <c>RfqLine.PurchaseRequisitionLineId</c>
    /// to the source Purchase Requisition's own line — the same field RFQ kept purely for traceability now
    /// earns its keep.</summary>
    private async Task AddLinesFromRfqAsync(PurchaseOrder po, Guid rfqId, Guid vendorId, CancellationToken cancellationToken)
    {
        var rfq = await _requestForQuotationRepository.GetAsync(rfqId, cancellationToken)
            ?? throw new ArgumentException($"Request for quotation {rfqId} was not found.");
        if (rfq.Status != Platform.Core.BusinessObjectStatus.Approved)
            throw new ArgumentException($"Request for quotation '{rfq.DocumentNumber}' must be Approved before a purchase order can be created from it.");
        if (!rfq.InvitedVendors.Any(v => v.VendorId == vendorId))
            throw new ArgumentException($"Business partner {vendorId} was not invited to request for quotation '{rfq.DocumentNumber}'.");

        var purchaseRequisition = await _purchaseRequisitionRepository.GetAsync(rfq.PurchaseRequisitionId, cancellationToken)
            ?? throw new ArgumentException($"Purchase requisition {rfq.PurchaseRequisitionId} was not found.");

        foreach (var rfqLine in rfq.Lines)
        {
            var quote = rfq.VendorQuoteLines.FirstOrDefault(q => q.VendorId == vendorId && q.RfqLineId == rfqLine.Id)
                ?? throw new ArgumentException($"Business partner {vendorId} has not quoted every line of request for quotation '{rfq.DocumentNumber}'.");
            var costCenterId = purchaseRequisition.Lines.First(l => l.Id == rfqLine.PurchaseRequisitionLineId).CostCenterId;
            po.AddLine(rfqLine.ItemId, costCenterId, rfqLine.Quantity, quote.QuotedUnitPrice, rfqLine.Id);
        }
    }

    private async Task ValidateVendorAsync(Guid vendorId, CancellationToken cancellationToken)
    {
        var vendor = await _businessPartnerLookup.GetAsync(vendorId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {vendorId} was not found.");
        if (vendor.Status != "Approved")
            throw new ArgumentException($"Business partner '{vendor.Name}' is not Approved and cannot receive a purchase order.");
        if (!vendor.BusinessRoles.Any(VendorEligibleRoles.Contains))
            throw new ArgumentException($"Business partner '{vendor.Name}' does not hold a role eligible to receive a purchase order.");
    }

    /// <summary>Validates a direct line's Item and Cost Center reference through Modules.MasterData.Contracts
    /// — never through MasterData's own Domain/Infrastructure — same pattern as
    /// <c>PurchaseRequisitionService.ValidateLineReferencesAsync</c>.</summary>
    private async Task ValidateLineReferencesAsync(Guid itemId, Guid costCenterId, CancellationToken cancellationToken)
    {
        var item = await _itemLookup.GetAsync(itemId, cancellationToken)
            ?? throw new ArgumentException($"Item {itemId} was not found.");
        if (!item.IsActive)
            throw new ArgumentException($"Item '{item.ItemCode}' is not active.");

        var costCenter = await _costCenterLookup.GetAsync(costCenterId, cancellationToken)
            ?? throw new ArgumentException($"Cost center {costCenterId} was not found.");
        if (!costCenter.IsActive)
            throw new ArgumentException($"Cost center '{costCenter.CostCenterCode}' is not active.");
        if (!costCenter.IsPostable)
            throw new ArgumentException($"Cost center '{costCenter.CostCenterCode}' is a header/grouping cost center and cannot be charged.");
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static Platform.Core.BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<PurchaseOrder> RequirePurchaseOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order {id} was not found.");

    private static PurchaseOrderDto ToDto(PurchaseOrder po) => new(
        po.Id, po.DocumentNumber, po.Status.ToString(), po.VendorId, po.RequestForQuotationId, po.Total,
        po.Lines.Select(l => new PurchaseOrderLineDto(l.Id, l.ItemId, l.CostCenterId, l.Quantity, l.UnitPrice, l.LineTotal, l.RfqLineId)).ToList(),
        po.CreatedAt, po.CreatedBy);
}
