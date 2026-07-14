using System.Text.Json;
using Modules.MasterData.Contracts;
using Modules.Procurement.Domain;
using Platform.Audit;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Procurement.Application;

public sealed class PurchaseRequisitionService
{
    public const string NumberRangeKey = "PROC-PR";

    private const string AuditTargetType = "PurchaseRequisition";
    private const string AuditSource = "Modules.Procurement";

    private readonly IPurchaseRequisitionRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IItemLookup _itemLookup;
    private readonly ICostCenterLookup _costCenterLookup;

    public PurchaseRequisitionService(
        IPurchaseRequisitionRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IItemLookup itemLookup,
        ICostCenterLookup costCenterLookup)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _itemLookup = itemLookup;
        _costCenterLookup = costCenterLookup;
    }

    public async Task<PurchaseRequisitionDto> CreateAsync(
        CreatePurchaseRequisitionRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, PurchaseRequisitionSecurity.MaintainPrivilegeKey);

        if (request.Lines.Count == 0)
            throw new ArgumentException("A purchase requisition needs at least one line.");

        var requisition = new PurchaseRequisition(actor, request.Description, request.RequiredByDate);
        foreach (var line in request.Lines)
        {
            await ValidateLineReferencesAsync(line.ItemId, line.CostCenterId, cancellationToken);
            requisition.AddLine(line.ItemId, line.CostCenterId, line.Quantity, line.EstimatedUnitPrice, line.LineDescription);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        requisition.AssignNumber(documentNumber);

        _repository.Add(requisition);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(requisition.Id), actor,
            $"Purchase requisition '{requisition.DocumentNumber}' created ({requisition.Lines.Count} lines, estimated total {requisition.EstimatedTotal}).", AuditSource);

        return ToDto(requisition);
    }

    public async Task<PurchaseRequisitionDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requisition = await _repository.GetAsync(id, cancellationToken);
        return requisition is null ? null : ToDto(requisition);
    }

    public async Task<(IReadOnlyList<PurchaseRequisitionDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<PurchaseRequisitionDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, PurchaseRequisitionSecurity.MaintainPrivilegeKey);
        var requisition = await RequireRequisitionAsync(id, cancellationToken);

        var fromStatus = requisition.Status;
        requisition.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(requisition.Id), actor,
            $"Purchase requisition '{requisition.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(requisition.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(PurchaseRequisitionWorkflow.BusinessObjectType, PurchaseRequisitionWorkflow.SubmitTransition, requisition.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(requisition, actor, cancellationToken);
        }

        return ToDto(requisition);
    }

    public Task<PurchaseRequisitionDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<PurchaseRequisitionDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<PurchaseRequisitionDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, PurchaseRequisitionSecurity.ApprovePrivilegeKey);
        var requisition = await RequireRequisitionAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(PurchaseRequisitionWorkflow.BusinessObjectType, requisition.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase requisition {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(requisition, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(requisition, actor, cancellationToken);

        return ToDto(requisition);
    }

    private async Task ApproveInternalAsync(PurchaseRequisition requisition, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = requisition.Status;
        requisition.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(requisition.Id), actor,
            $"Purchase requisition '{requisition.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(requisition.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(PurchaseRequisition requisition, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = requisition.Status;
        requisition.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(requisition.Id), actor,
            $"Purchase requisition '{requisition.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(requisition.Status.ToString()), AuditSource);
    }

    /// <summary>Validates a line's Item (and Cost Center) reference through Modules.MasterData.Contracts —
    /// never through MasterData's own Domain/Infrastructure — before the line is ever added. Both must
    /// exist and be Active; the cost center must also be Postable (a header/grouping cost center can never
    /// receive a charge).</summary>
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

    private async Task<PurchaseRequisition> RequireRequisitionAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase requisition {id} was not found.");

    private static PurchaseRequisitionDto ToDto(PurchaseRequisition r) => new(
        r.Id, r.DocumentNumber, r.Status.ToString(), r.Description, r.RequiredByDate, r.EstimatedTotal,
        r.Lines.Select(l => new PurchaseRequisitionLineDto(
            l.Id, l.ItemId, l.CostCenterId, l.Quantity, l.EstimatedUnitPrice, l.EstimatedLineTotal, l.LineDescription)).ToList(),
        r.CreatedAt, r.CreatedBy);
}
