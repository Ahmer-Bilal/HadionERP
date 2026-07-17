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
/// Orchestrates <see cref="Ipc"/> — generated entirely from an Approved <see cref="MeasurementSheet"/> plus
/// the commercial document (Contract or Subcontract) it measures against, never from caller-supplied lines,
/// since every figure on an IPC is derived (construction-commercial-processes-spec.md §3), not entered by
/// hand. Resolves the same Contract/Subcontract polymorphism <see cref="MeasurementSheetService"/> does, for
/// the same reason (both live in this module, so no cross-module lookup is needed).
/// </summary>
public sealed class IpcService
{
    public const string NumberRangeKey = "CON-IPC";

    private const string AuditTargetType = "Ipc";
    private const string AuditSource = "Modules.Construction";

    private readonly IIpcRepository _repository;
    private readonly IMeasurementSheetRepository _measurementSheetRepository;
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

    public IpcService(
        IIpcRepository repository,
        IMeasurementSheetRepository measurementSheetRepository,
        IContractRepository contractRepository,
        ISubcontractRepository subcontractRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IProjectLookup projectLookup,
        ICustomerInvoicingService customerInvoicingService)
    {
        _repository = repository;
        _measurementSheetRepository = measurementSheetRepository;
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
    }

    public async Task<IpcDto> CreateAsync(
        CreateIpcRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, IpcSecurity.MaintainPrivilegeKey);

        if (!Enum.TryParse<CommercialDocumentType>(request.CommercialDocumentType, ignoreCase: true, out var documentType))
            throw new ArgumentException(
                $"'{request.CommercialDocumentType}' is not a known commercial document type (Contract or Subcontract).");
        if (request.OtherDeductions < 0)
            throw new ArgumentException("Other deductions cannot be negative.", nameof(request.OtherDeductions));

        // Only a Contract-type IPC ever raises an AR Invoice on certification (a Subcontract IPC is a
        // payable to a subcontractor, a separate not-yet-built AP integration) — but the billing accounts
        // must be chosen now, at Draft time, not guessed later at certification.
        if (documentType == CommercialDocumentType.Contract)
        {
            if (request.RevenueAccountId is null || request.ReceivableAccountId is null)
                throw new ArgumentException(
                    "A Revenue account and a Receivable account are required for a Contract IPC, since certifying it raises a real AR Invoice.");
            if (request.TaxCodeId is not null && request.VatAccountId is null)
                throw new ArgumentException("A VAT account is required when a tax code is specified.");

            var project = await _projectLookup.GetAsync(request.ProjectId, cancellationToken)
                ?? throw new ArgumentException($"Project {request.ProjectId} was not found.");
            if (project.CustomerId is null)
                throw new ArgumentException(
                    $"Project '{project.ProjectName}' has no Customer set — an AR Invoice cannot be raised without one.");
        }

        var sheet = await _measurementSheetRepository.GetAsync(request.MeasurementSheetId, cancellationToken)
            ?? throw new ArgumentException($"Measurement sheet {request.MeasurementSheetId} was not found.");
        if (sheet.Status != BusinessObjectStatus.Approved)
            throw new ArgumentException(
                $"Measurement sheet '{sheet.DocumentNumber}' is not Approved (Certified) and cannot be billed.");
        if (sheet.ProjectId != request.ProjectId || sheet.CommercialDocumentType != documentType || sheet.CommercialDocumentId != request.CommercialDocumentId)
            throw new ArgumentException(
                $"Measurement sheet '{sheet.DocumentNumber}' does not match the given project/commercial document.");
        if (await _repository.ExistsForMeasurementSheetAsync(sheet.Id, cancellationToken))
            throw new ArgumentException($"An IPC has already been raised against measurement sheet '{sheet.DocumentNumber}'.");

        var document = await LoadCommercialDocumentAsync(documentType, request.CommercialDocumentId, cancellationToken);
        if (document.Status != BusinessObjectStatus.Approved)
            throw new ArgumentException($"{documentType} '{document.Label}' is not Approved and cannot be billed.");

        var siblingSheets = await _measurementSheetRepository.ListByCommercialDocumentAsync(
            documentType, request.CommercialDocumentId, cancellationToken);

        var ipc = new Ipc(
            actor, request.ProjectId, documentType, request.CommercialDocumentId, sheet.Id,
            sheet.PeriodStart, sheet.PeriodEnd, document.RetentionPercentage, document.AdvancePaymentPercentage,
            request.OtherDeductions, request.RevenueAccountId, request.ReceivableAccountId, request.TaxCodeId, request.VatAccountId);

        foreach (var line in sheet.Lines)
        {
            if (!document.Lines.TryGetValue(line.CommercialDocumentLineId, out var documentLine))
                throw new ArgumentException(
                    $"Line {line.CommercialDocumentLineId} no longer exists on this {documentType}.");

            var quantityToDate = siblingSheets
                .Where(s => s.Status == BusinessObjectStatus.Approved)
                .SelectMany(s => s.Lines)
                .Where(l => l.CommercialDocumentLineId == line.CommercialDocumentLineId)
                .Sum(l => l.QuantityCertified ?? 0m);

            ipc.AddLine(line.CommercialDocumentLineId, documentLine.Rate, line.QuantityCertified ?? 0m, quantityToDate);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        ipc.AssignNumber(documentNumber);

        _repository.Add(ipc);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(ipc.Id), actor,
            $"IPC '{ipc.DocumentNumber}' created against {documentType} '{document.Label}' from measurement sheet '{sheet.DocumentNumber}' " +
            $"(net payable {ipc.NetPayable}).", AuditSource);

        return ToDto(ipc);
    }

    public async Task<IpcDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ipc = await _repository.GetAsync(id, cancellationToken);
        return ipc is null ? null : ToDto(ipc);
    }

    public async Task<(IReadOnlyList<IpcDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<IpcDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, IpcSecurity.MaintainPrivilegeKey);
        var ipc = await RequireIpcAsync(id, cancellationToken);

        var fromStatus = ipc.Status;
        ipc.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(ipc.Id), actor,
            $"IPC '{ipc.DocumentNumber}' submitted for certification.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(ipc.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(IpcWorkflow.BusinessObjectType, IpcWorkflow.SubmitTransition, ipc.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(ipc, actor, cancellationToken);
        }

        return ToDto(ipc);
    }

    public Task<IpcDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<IpcDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<IpcDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, IpcSecurity.ApprovePrivilegeKey);
        var ipc = await RequireIpcAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(IpcWorkflow.BusinessObjectType, ipc.Id, cancellationToken)
            ?? throw new InvalidOperationException($"IPC {id} has no pending certification to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(ipc, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(ipc, actor, cancellationToken);

        return ToDto(ipc);
    }

    /// <summary>Certifies the IPC and, for a Contract-type IPC, raises the real AR Invoice first — see
    /// <see cref="Ipc"/>'s own doc comment for why this happens here (certification is the moment the
    /// spec says the customer becomes legally obligated to pay) and why it's left in Draft rather than
    /// auto-posted. Runs before <c>ipc.Approve</c> deliberately: if raising the invoice fails (e.g. the
    /// customer was deactivated after this IPC was created), the IPC itself must not silently certify with
    /// no invoice behind it.</summary>
    private async Task ApproveInternalAsync(Ipc ipc, string actor, CancellationToken cancellationToken)
    {
        if (ipc.CommercialDocumentType == CommercialDocumentType.Contract
            && ipc.RevenueAccountId is { } revenueAccountId && ipc.ReceivableAccountId is { } receivableAccountId)
        {
            var project = await _projectLookup.GetAsync(ipc.ProjectId, cancellationToken)
                ?? throw new InvalidOperationException($"Project {ipc.ProjectId} no longer exists.");
            if (project.CustomerId is not { } customerId)
                throw new InvalidOperationException($"Project '{project.ProjectName}' no longer has a Customer set.");

            var arInvoiceId = await _customerInvoicingService.RaiseInvoiceAsync(
                new RaiseCustomerInvoiceRequest(
                    customerId, ipc.DocumentNumber, ipc.PeriodEnd, $"IPC {ipc.DocumentNumber} — {project.ProjectName}",
                    revenueAccountId, receivableAccountId, ipc.NetPayable, CostCenterId: null, ipc.TaxCodeId, ipc.VatAccountId),
                actor, "C001", cancellationToken);
            ipc.LinkArInvoice(arInvoiceId);
        }

        var fromStatus = ipc.Status;
        ipc.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(ipc.Id), actor,
            ipc.LinkedArInvoiceId is { } linkedArInvoiceId
                ? $"IPC '{ipc.DocumentNumber}' certified (net payable {ipc.NetPayable}), AR Invoice {linkedArInvoiceId} raised."
                : $"IPC '{ipc.DocumentNumber}' certified (net payable {ipc.NetPayable}).",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(ipc.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(Ipc ipc, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = ipc.Status;
        ipc.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(ipc.Id), actor,
            $"IPC '{ipc.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(ipc.Status.ToString()), AuditSource);
    }

    private async Task<CommercialDocumentSnapshot> LoadCommercialDocumentAsync(
        CommercialDocumentType type, Guid id, CancellationToken cancellationToken)
    {
        if (type == CommercialDocumentType.Contract)
        {
            var contract = await _contractRepository.GetAsync(id, cancellationToken)
                ?? throw new ArgumentException($"Contract {id} was not found.");
            return new CommercialDocumentSnapshot(
                contract.Status, contract.DocumentNumber, RetentionPercentage: null, contract.AdvancePaymentPercentage,
                contract.BoqLines.ToDictionary(l => l.Id, l => new DocumentLineSnapshot(l.Rate)));
        }

        var subcontract = await _subcontractRepository.GetAsync(id, cancellationToken)
            ?? throw new ArgumentException($"Subcontract {id} was not found.");
        return new CommercialDocumentSnapshot(
            subcontract.Status, subcontract.DocumentNumber, subcontract.RetentionPercentage, subcontract.MobilizationAdvancePercentage,
            subcontract.Lines.ToDictionary(l => l.Id, l => new DocumentLineSnapshot(l.Rate)));
    }

    private sealed record DocumentLineSnapshot(decimal Rate);

    private sealed record CommercialDocumentSnapshot(
        BusinessObjectStatus Status, string? Label, decimal? RetentionPercentage, decimal? AdvancePaymentPercentage,
        IReadOnlyDictionary<Guid, DocumentLineSnapshot> Lines);

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<Ipc> RequireIpcAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"IPC {id} was not found.");

    private static IpcDto ToDto(Ipc i) => new(
        i.Id, i.DocumentNumber, i.Status.ToString(), i.ProjectId, i.CommercialDocumentType.ToString(), i.CommercialDocumentId,
        i.MeasurementSheetId, i.PeriodStart, i.PeriodEnd, i.RetentionPercentageApplied, i.AdvancePaymentPercentageApplied,
        i.OtherDeductions, i.RevenueAccountId, i.ReceivableAccountId, i.TaxCodeId, i.VatAccountId, i.LinkedArInvoiceId,
        i.GrossValueToDate, i.GrossValueThisPeriod, i.GrossValuePreviousIpc, i.RetentionAmount,
        i.AdvanceRecoveryAmount, i.NetPayable,
        i.Lines.Select(l => new IpcLineDto(l.Id, l.CommercialDocumentLineId, l.Rate, l.QuantityThisPeriod, l.QuantityToDate, l.ValueThisPeriod, l.ValueToDate)).ToList(),
        i.CreatedAt, i.CreatedBy);
}
