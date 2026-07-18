using System.Text.Json;
using Modules.Construction.Domain;
using Modules.Finance.Contracts;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Construction.Application;

/// <summary>
/// Orchestrates <see cref="RetentionRelease"/> — validates the requested amount against the real running
/// retention balance (every Approved <see cref="Ipc"/>'s own <c>RetentionAmount</c> for this commercial
/// document, less every prior Approved release) before ever letting one be created, the over-release guard
/// this document type exists to enforce (construction-commercial-processes-spec.md §5). Resolves the same
/// Contract/Subcontract polymorphism <see cref="IpcService"/> does, for the same reason (both live in this
/// module, so no cross-module lookup is needed).
/// </summary>
public sealed class RetentionReleaseService
{
    public const string NumberRangeKey = "CON-RETREL";

    private const string AuditTargetType = "RetentionRelease";
    private const string AuditSource = "Modules.Construction";

    private readonly IRetentionReleaseRepository _repository;
    private readonly IIpcRepository _ipcRepository;
    private readonly IContractRepository _contractRepository;
    private readonly ISubcontractRepository _subcontractRepository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IProjectLookup _projectLookup;
    private readonly ICustomerInvoicingService _customerInvoicingService;
    private readonly IVendorInvoicingService _vendorInvoicingService;

    public RetentionReleaseService(
        IRetentionReleaseRepository repository,
        IIpcRepository ipcRepository,
        IContractRepository contractRepository,
        ISubcontractRepository subcontractRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IProjectLookup projectLookup,
        ICustomerInvoicingService customerInvoicingService,
        IVendorInvoicingService vendorInvoicingService)
    {
        _repository = repository;
        _ipcRepository = ipcRepository;
        _contractRepository = contractRepository;
        _subcontractRepository = subcontractRepository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _projectLookup = projectLookup;
        _customerInvoicingService = customerInvoicingService;
        _vendorInvoicingService = vendorInvoicingService;
    }

    public async Task<RetentionReleaseDto> CreateAsync(
        CreateRetentionReleaseRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, RetentionReleaseSecurity.MaintainPrivilegeKey);

        if (!Enum.TryParse<CommercialDocumentType>(request.CommercialDocumentType, ignoreCase: true, out var documentType))
            throw new ArgumentException(
                $"'{request.CommercialDocumentType}' is not a known commercial document type (Contract or Subcontract).");
        if (!Enum.TryParse<RetentionTriggerEvent>(request.TriggerEvent, ignoreCase: true, out var triggerEvent))
            throw new ArgumentException(
                $"'{request.TriggerEvent}' is not a known retention release trigger event (TakingOver, DefectsLiabilityExpiry, Manual).");
        if (request.AmountReleased <= 0)
            throw new ArgumentException("Amount released must be greater than zero.", nameof(request.AmountReleased));

        // A Contract-type release raises an AR Invoice on approval (the customer owes the contractor back);
        // a Subcontract-type release raises an AP Invoice (the main contractor owes the subcontractor back)
        // — same billing-account requirement IpcService.CreateAsync already enforces.
        if (documentType == CommercialDocumentType.Contract)
        {
            if (request.RevenueAccountId is null || request.ReceivableAccountId is null)
                throw new ArgumentException(
                    "A Revenue account and a Receivable account are required for a Contract retention release, since approving it raises a real AR Invoice.");
            if (request.TaxCodeId is not null && request.VatAccountId is null)
                throw new ArgumentException("A VAT account is required when a tax code is specified.");

            var project = await _projectLookup.GetAsync(request.ProjectId, cancellationToken)
                ?? throw new ArgumentException($"Project {request.ProjectId} was not found.");
            if (project.CustomerId is null)
                throw new ArgumentException(
                    $"Project '{project.ProjectName}' has no Customer set — an AR Invoice cannot be raised without one.");
        }
        else
        {
            if (request.ExpenseAccountId is null || request.PayableAccountId is null)
                throw new ArgumentException(
                    "An Expense account and a Payable account are required for a Subcontract retention release, since approving it raises a real AP Invoice.");
            if (request.TaxCodeId is not null && request.VatAccountId is null)
                throw new ArgumentException("A VAT account is required when a tax code is specified.");
        }

        var document = await LoadCommercialDocumentAsync(documentType, request.CommercialDocumentId, cancellationToken);
        if (document.Status != BusinessObjectStatus.Approved)
            throw new ArgumentException($"{documentType} '{document.Label}' is not Approved — retention can only be released against a live document.");

        var balance = await GetRetentionBalanceInternalAsync(documentType, request.CommercialDocumentId, cancellationToken);
        if (request.AmountReleased > balance.OutstandingBalance)
            throw new ArgumentException(
                $"Amount released ({request.AmountReleased}) exceeds the outstanding retention balance " +
                $"({balance.OutstandingBalance}) withheld against this {documentType}.");

        var release = new RetentionRelease(
            actor, request.ProjectId, documentType, request.CommercialDocumentId, request.ReleaseDate, request.AmountReleased, triggerEvent,
            request.RevenueAccountId, request.ReceivableAccountId, request.TaxCodeId, request.VatAccountId,
            request.ExpenseAccountId, request.PayableAccountId);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        release.AssignNumber(documentNumber);

        _repository.Add(release);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(release.Id), actor,
            $"Retention release '{release.DocumentNumber}' created against {documentType} '{document.Label}' " +
            $"(amount {release.AmountReleased}, outstanding balance was {balance.OutstandingBalance}).", AuditSource);

        return ToDto(release);
    }

    public async Task<RetentionReleaseDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var release = await _repository.GetAsync(id, cancellationToken);
        return release is null ? null : ToDto(release);
    }

    public async Task<(IReadOnlyList<RetentionReleaseDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    /// <summary>Public query behind the "show the balance before you release against it" UI affordance — the
    /// same computation <see cref="CreateAsync"/> validates a new release's amount against.</summary>
    public async Task<RetentionBalanceDto> GetRetentionBalanceAsync(
        string commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CommercialDocumentType>(commercialDocumentType, ignoreCase: true, out var documentType))
            throw new ArgumentException(
                $"'{commercialDocumentType}' is not a known commercial document type (Contract or Subcontract).");

        return await GetRetentionBalanceInternalAsync(documentType, commercialDocumentId, cancellationToken);
    }

    public async Task<RetentionReleaseDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, RetentionReleaseSecurity.MaintainPrivilegeKey);
        var release = await RequireReleaseAsync(id, cancellationToken);

        var fromStatus = release.Status;
        release.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(release.Id), actor,
            $"Retention release '{release.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(release.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(RetentionReleaseWorkflow.BusinessObjectType, RetentionReleaseWorkflow.SubmitTransition, release.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(release, actor, cancellationToken);
        }

        return ToDto(release);
    }

    public Task<RetentionReleaseDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<RetentionReleaseDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<RetentionReleaseDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, RetentionReleaseSecurity.ApprovePrivilegeKey);
        var release = await RequireReleaseAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(RetentionReleaseWorkflow.BusinessObjectType, release.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Retention release {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(release, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(release, actor, cancellationToken);

        return ToDto(release);
    }

    /// <summary>Approves the release and raises the real invoice first — a Contract-type release raises an
    /// AR Invoice against the Project's Customer, a Subcontract-type release raises an AP Invoice against
    /// the Subcontract's own Subcontractor. Mirrors <c>IpcService.ApproveInternalAsync</c> exactly, including
    /// running before <c>release.Approve</c> so a failure to raise the invoice never leaves the release
    /// silently approved with no invoice behind it.</summary>
    private async Task ApproveInternalAsync(RetentionRelease release, string actor, CancellationToken cancellationToken)
    {
        if (release.CommercialDocumentType == CommercialDocumentType.Contract
            && release.RevenueAccountId is { } revenueAccountId && release.ReceivableAccountId is { } receivableAccountId)
        {
            var project = await _projectLookup.GetAsync(release.ProjectId, cancellationToken)
                ?? throw new InvalidOperationException($"Project {release.ProjectId} no longer exists.");
            if (project.CustomerId is not { } customerId)
                throw new InvalidOperationException($"Project '{project.ProjectName}' no longer has a Customer set.");

            var arInvoiceId = await _customerInvoicingService.RaiseInvoiceAsync(
                new RaiseCustomerInvoiceRequest(
                    customerId, release.DocumentNumber, release.ReleaseDate,
                    $"Retention release {release.DocumentNumber} — {project.ProjectName}",
                    revenueAccountId, receivableAccountId, release.AmountReleased, CostCenterId: null, release.TaxCodeId, release.VatAccountId,
                    SourceDocumentType: "RetentionRelease", SourceDocumentId: release.Id),
                actor, "C001", cancellationToken);
            release.LinkArInvoice(arInvoiceId);
        }
        else if (release.CommercialDocumentType == CommercialDocumentType.Subcontract
            && release.ExpenseAccountId is { } expenseAccountId && release.PayableAccountId is { } payableAccountId)
        {
            var subcontract = await _subcontractRepository.GetAsync(release.CommercialDocumentId, cancellationToken)
                ?? throw new InvalidOperationException($"Subcontract {release.CommercialDocumentId} no longer exists.");

            var apInvoiceId = await _vendorInvoicingService.RaiseInvoiceAsync(
                new RaiseVendorInvoiceRequest(
                    subcontract.SubcontractorId, release.DocumentNumber!, release.ReleaseDate,
                    $"Retention release {release.DocumentNumber} — {subcontract.DocumentNumber}",
                    expenseAccountId, payableAccountId, release.AmountReleased, CostCenterId: null, release.TaxCodeId, release.VatAccountId,
                    SourceDocumentType: "RetentionRelease", SourceDocumentId: release.Id),
                actor, "C001", cancellationToken);
            release.LinkApInvoice(apInvoiceId);
        }

        var fromStatus = release.Status;
        release.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(release.Id), actor,
            release.LinkedArInvoiceId is { } linkedArInvoiceId
                ? $"Retention release '{release.DocumentNumber}' approved (amount {release.AmountReleased}), AR Invoice {linkedArInvoiceId} raised."
                : release.LinkedApInvoiceId is { } linkedApInvoiceId
                    ? $"Retention release '{release.DocumentNumber}' approved (amount {release.AmountReleased}), AP Invoice {linkedApInvoiceId} raised."
                    : $"Retention release '{release.DocumentNumber}' approved (amount {release.AmountReleased}).",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(release.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(RetentionRelease release, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = release.Status;
        release.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(release.Id), actor,
            $"Retention release '{release.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(release.Status.ToString()), AuditSource);
    }

    private async Task<RetentionBalanceDto> GetRetentionBalanceInternalAsync(
        CommercialDocumentType documentType, Guid commercialDocumentId, CancellationToken cancellationToken)
    {
        var ipcs = await _ipcRepository.ListByCommercialDocumentAsync(documentType, commercialDocumentId, cancellationToken);
        var totalWithheld = ipcs.Where(i => i.Status == BusinessObjectStatus.Approved).Sum(i => i.RetentionAmount);

        var releases = await _repository.ListByCommercialDocumentAsync(documentType, commercialDocumentId, cancellationToken);
        var totalReleased = releases.Where(r => r.Status == BusinessObjectStatus.Approved).Sum(r => r.AmountReleased);

        return new RetentionBalanceDto(commercialDocumentId, totalWithheld, totalReleased, totalWithheld - totalReleased);
    }

    private async Task<CommercialDocumentSnapshot> LoadCommercialDocumentAsync(
        CommercialDocumentType type, Guid id, CancellationToken cancellationToken)
    {
        if (type == CommercialDocumentType.Contract)
        {
            var contract = await _contractRepository.GetAsync(id, cancellationToken)
                ?? throw new ArgumentException($"Contract {id} was not found.");
            return new CommercialDocumentSnapshot(contract.Status, contract.DocumentNumber);
        }

        var subcontract = await _subcontractRepository.GetAsync(id, cancellationToken)
            ?? throw new ArgumentException($"Subcontract {id} was not found.");
        return new CommercialDocumentSnapshot(subcontract.Status, subcontract.DocumentNumber);
    }

    private sealed record CommercialDocumentSnapshot(BusinessObjectStatus Status, string? Label);

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<RetentionRelease> RequireReleaseAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Retention release {id} was not found.");

    private static RetentionReleaseDto ToDto(RetentionRelease r) => new(
        r.Id, r.DocumentNumber, r.Status.ToString(), r.ProjectId, r.CommercialDocumentType.ToString(), r.CommercialDocumentId,
        r.ReleaseDate, r.AmountReleased, r.TriggerEvent.ToString(),
        r.RevenueAccountId, r.ReceivableAccountId, r.ExpenseAccountId, r.PayableAccountId,
        r.TaxCodeId, r.VatAccountId, r.LinkedArInvoiceId, r.LinkedApInvoiceId,
        r.CreatedAt, r.CreatedBy);
}
