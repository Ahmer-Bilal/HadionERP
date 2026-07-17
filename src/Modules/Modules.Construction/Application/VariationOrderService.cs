using System.Text.Json;
using Modules.Construction.Domain;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Construction.Application;

/// <summary>
/// Orchestrates <see cref="VariationOrder"/> — same "resolves polymorphism over commercial document at the
/// Application layer" pattern as <see cref="MeasurementSheetService"/>/<c>IpcService</c>, since the Domain
/// type has no dependency on <see cref="IContractRepository"/>/<see cref="ISubcontractRepository"/>.
/// Approval is the one place this module writes to an already-Approved Contract/Subcontract's own lines —
/// see <see cref="ApproveInternalAsync"/>.
/// </summary>
public sealed class VariationOrderService
{
    public const string NumberRangeKey = "CON-VO";

    private const string AuditTargetType = "VariationOrder";
    private const string AuditSource = "Modules.Construction";

    private readonly IVariationOrderRepository _repository;
    private readonly IContractRepository _contractRepository;
    private readonly ISubcontractRepository _subcontractRepository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IProjectLookup _projectLookup;

    public VariationOrderService(
        IVariationOrderRepository repository,
        IContractRepository contractRepository,
        ISubcontractRepository subcontractRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IProjectLookup projectLookup)
    {
        _repository = repository;
        _contractRepository = contractRepository;
        _subcontractRepository = subcontractRepository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _projectLookup = projectLookup;
    }

    public async Task<VariationOrderDto> CreateAsync(
        CreateVariationOrderRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, VariationOrderSecurity.MaintainPrivilegeKey);

        if (request.Lines.Count == 0)
            throw new ArgumentException("A variation order needs at least one line.");
        if (!Enum.TryParse<CommercialDocumentType>(request.CommercialDocumentType, ignoreCase: true, out var documentType))
            throw new ArgumentException(
                $"'{request.CommercialDocumentType}' is not a known commercial document type (Contract or Subcontract).");

        var project = await _projectLookup.GetAsync(request.ProjectId, cancellationToken)
            ?? throw new ArgumentException($"Project {request.ProjectId} was not found.");
        if (project.Status != "Approved")
            throw new ArgumentException($"Project '{project.ProjectName}' is not Approved (status: {project.Status}).");

        var document = await LoadCommercialDocumentAsync(documentType, request.CommercialDocumentId, cancellationToken);
        if (document.ProjectId != request.ProjectId)
            throw new ArgumentException($"{documentType} '{document.Label}' does not belong to project '{project.ProjectName}'.");
        if (document.Status != BusinessObjectStatus.Approved)
            throw new ArgumentException($"{documentType} '{document.Label}' is not Approved and cannot be varied.");

        var wbsElementsById = project.WbsElements.ToDictionary(w => w.Id);

        var order = new VariationOrder(actor, request.ProjectId, documentType, request.CommercialDocumentId, request.Reason);

        foreach (var lineRequest in request.Lines)
        {
            if (lineRequest.CommercialDocumentLineId is { } lineId)
            {
                if (lineRequest.Code is not null || lineRequest.Description is not null
                    || lineRequest.UnitOfMeasure is not null || lineRequest.WbsElementId is not null || lineRequest.Rate is not null)
                    throw new ArgumentException(
                        "A line adjusting an existing commercial document line cannot also carry new-line fields.");
                if (!document.Lines.TryGetValue(lineId, out var documentLine))
                    throw new ArgumentException($"Line {lineId} does not belong to this {documentType}.");

                order.AddLineAdjustment(lineId, lineRequest.QuantityDelta, documentLine.Rate);
            }
            else
            {
                if (lineRequest.Code is null || lineRequest.Description is null
                    || lineRequest.UnitOfMeasure is null || lineRequest.WbsElementId is null || lineRequest.Rate is null)
                    throw new ArgumentException(
                        "A new line requires Code, Description, UnitOfMeasure, WbsElementId, and Rate.");
                if (!wbsElementsById.ContainsKey(lineRequest.WbsElementId.Value))
                    throw new ArgumentException($"WBS element {lineRequest.WbsElementId} does not belong to project '{project.ProjectName}'.");

                order.AddNewLine(
                    lineRequest.Code, lineRequest.Description, lineRequest.DescriptionArabic, lineRequest.UnitOfMeasure,
                    lineRequest.QuantityDelta, lineRequest.Rate.Value, lineRequest.WbsElementId.Value);
            }
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        order.AssignNumber(documentNumber);

        _repository.Add(order);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(order.Id), actor,
            $"Variation order '{order.DocumentNumber}' created against {documentType} '{document.Label}' (total value {order.TotalValue}).",
            AuditSource);

        return ToDto(order);
    }

    public async Task<VariationOrderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetAsync(id, cancellationToken);
        return order is null ? null : ToDto(order);
    }

    public async Task<(IReadOnlyList<VariationOrderDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<VariationOrderDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, VariationOrderSecurity.MaintainPrivilegeKey);
        var order = await RequireOrderAsync(id, cancellationToken);

        var fromStatus = order.Status;
        order.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(order.Id), actor,
            $"Variation order '{order.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(order.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(VariationOrderWorkflow.BusinessObjectType, VariationOrderWorkflow.SubmitTransition, order.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(order, actor, cancellationToken);
        }

        return ToDto(order);
    }

    public Task<VariationOrderDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<VariationOrderDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<VariationOrderDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, VariationOrderSecurity.ApprovePrivilegeKey);
        var order = await RequireOrderAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(VariationOrderWorkflow.BusinessObjectType, order.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Variation order {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(order, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(order, actor, cancellationToken);

        return ToDto(order);
    }

    /// <summary>Writes the approved change through to the underlying Contract/Subcontract's own lines before
    /// approving the order itself — if the write-through fails (e.g. an adjustment would drop a line's
    /// quantity to zero), the order must not silently approve with no effect behind it, same ordering
    /// discipline as <c>IpcService.ApproveInternalAsync</c> raising its invoice before certifying.</summary>
    private async Task ApproveInternalAsync(VariationOrder order, string actor, CancellationToken cancellationToken)
    {
        if (order.CommercialDocumentType == CommercialDocumentType.Contract)
        {
            var contract = await _contractRepository.GetAsync(order.CommercialDocumentId, cancellationToken)
                ?? throw new InvalidOperationException($"Contract {order.CommercialDocumentId} no longer exists.");
            foreach (var line in order.Lines)
            {
                if (line.CommercialDocumentLineId is { } lineId)
                    contract.AdjustBoqLineQuantity(lineId, line.QuantityDelta);
                else
                    contract.AddBoqLineFromVariationOrder(
                        line.Code!, line.Description!, line.DescriptionArabic, line.UnitOfMeasure!,
                        line.QuantityDelta, line.Rate, line.WbsElementId!.Value);
            }
            await _contractRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var subcontract = await _subcontractRepository.GetAsync(order.CommercialDocumentId, cancellationToken)
                ?? throw new InvalidOperationException($"Subcontract {order.CommercialDocumentId} no longer exists.");
            foreach (var line in order.Lines)
            {
                if (line.CommercialDocumentLineId is { } lineId)
                    subcontract.AdjustLineQuantity(lineId, line.QuantityDelta);
                else
                    subcontract.AddLineFromVariationOrder(
                        line.Code!, line.Description!, line.DescriptionArabic, line.UnitOfMeasure!,
                        line.QuantityDelta, line.Rate, line.WbsElementId!.Value);
            }
            await _subcontractRepository.SaveChangesAsync(cancellationToken);
        }

        var fromStatus = order.Status;
        order.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(order.Id), actor,
            $"Variation order '{order.DocumentNumber}' approved (total value {order.TotalValue}) and applied to {order.CommercialDocumentType} {order.CommercialDocumentId}.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(order.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(VariationOrder order, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = order.Status;
        order.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(order.Id), actor,
            $"Variation order '{order.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(order.Status.ToString()), AuditSource);
    }

    private async Task<CommercialDocumentSnapshot> LoadCommercialDocumentAsync(
        CommercialDocumentType type, Guid id, CancellationToken cancellationToken)
    {
        if (type == CommercialDocumentType.Contract)
        {
            var contract = await _contractRepository.GetAsync(id, cancellationToken)
                ?? throw new ArgumentException($"Contract {id} was not found.");
            return new CommercialDocumentSnapshot(
                contract.ProjectId, contract.Status, contract.DocumentNumber,
                contract.BoqLines.ToDictionary(l => l.Id, l => (l.WbsElementId, l.Rate)));
        }

        var subcontract = await _subcontractRepository.GetAsync(id, cancellationToken)
            ?? throw new ArgumentException($"Subcontract {id} was not found.");
        return new CommercialDocumentSnapshot(
            subcontract.ProjectId, subcontract.Status, subcontract.DocumentNumber,
            subcontract.Lines.ToDictionary(l => l.Id, l => (l.WbsElementId, l.Rate)));
    }

    private sealed record CommercialDocumentSnapshot(
        Guid ProjectId, BusinessObjectStatus Status, string? Label,
        IReadOnlyDictionary<Guid, (Guid WbsElementId, decimal Rate)> Lines);

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<VariationOrder> RequireOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Variation order {id} was not found.");

    private static VariationOrderDto ToDto(VariationOrder o) => new(
        o.Id, o.DocumentNumber, o.Status.ToString(), o.ProjectId, o.CommercialDocumentType.ToString(), o.CommercialDocumentId,
        o.Reason, o.TotalValue,
        o.Lines.Select(l => new VariationOrderLineDto(
            l.Id, l.CommercialDocumentLineId, l.Code, l.Description, l.UnitOfMeasure, l.WbsElementId, l.QuantityDelta, l.Rate, l.Amount)).ToList(),
        o.CreatedAt, o.CreatedBy);
}
