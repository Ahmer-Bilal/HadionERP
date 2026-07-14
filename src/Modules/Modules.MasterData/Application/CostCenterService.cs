using System.Text.Json;
using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.MasterData.Application;

public sealed class CostCenterService
{
    public const string NumberRangeKey = "MD-CC";

    private const string AuditTargetType = "CostCenter";
    private const string AuditSource = "Modules.MasterData";

    private readonly ICostCenterRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;

    public CostCenterService(
        ICostCenterRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
    }

    public async Task<CostCenterDto> CreateAsync(
        CreateCostCenterRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, CostCenterSecurity.MaintainPrivilegeKey);

        var existing = await _repository.GetByCodeAsync(request.CostCenterCode, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Cost center code '{request.CostCenterCode}' is already in use.");

        var costCenter = new CostCenter(actor, request.CostCenterCode, request.CostCenterName);
        costCenter.UpdateCostCenterNameArabic(request.CostCenterNameArabic);
        costCenter.AssignParent(request.ParentCostCenterId);
        if (!request.IsPostable) costCenter.SetPostable(false);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        costCenter.AssignNumber(documentNumber);

        _repository.Add(costCenter);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(costCenter.Id), actor,
            $"Cost center '{costCenter.CostCenterCode}' ({costCenter.CostCenterName}) created.", AuditSource);

        return ToDto(costCenter);
    }

    public async Task<CostCenterDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var costCenter = await _repository.GetAsync(id, cancellationToken);
        return costCenter is null ? null : ToDto(costCenter);
    }

    public async Task<(IReadOnlyList<CostCenterDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<CostCenterDto> UpdateAsync(
        Guid id, UpdateCostCenterRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, CostCenterSecurity.MaintainPrivilegeKey);
        var costCenter = await RequireCostCenterAsync(id, cancellationToken);

        costCenter.UpdateCostCenterName(request.CostCenterName);
        costCenter.UpdateCostCenterNameArabic(request.CostCenterNameArabic);
        costCenter.AssignParent(request.ParentCostCenterId);
        costCenter.SetPostable(request.IsPostable);
        if (request.IsActive) costCenter.Activate(); else costCenter.Deactivate();

        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(costCenter.Id), actor,
            $"Cost center '{costCenter.CostCenterCode}' updated.",
            new[]
            {
                new FieldValueChange("CostCenterName", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.CostCenterName)),
                new FieldValueChange("IsPostable", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.IsPostable)),
                new FieldValueChange("IsActive", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.IsActive)),
            },
            AuditSource);

        return ToDto(costCenter);
    }

    public async Task<CostCenterDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, CostCenterSecurity.MaintainPrivilegeKey);
        var costCenter = await RequireCostCenterAsync(id, cancellationToken);
        var fromStatus = costCenter.Status;
        costCenter.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(costCenter.Id), actor,
            $"Cost center '{costCenter.CostCenterCode}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(costCenter.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(CostCenterWorkflow.BusinessObjectType, CostCenterWorkflow.SubmitTransition, costCenter.Id);
        if (instance is null) { await ApproveInternalAsync(costCenter, actor, cancellationToken); return ToDto(costCenter); }

        _workflowInstanceRepository.Add(instance);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(costCenter, actor, cancellationToken);

        return ToDto(costCenter);
    }

    public Task<CostCenterDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<CostCenterDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<CostCenterDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, CostCenterSecurity.ApprovePrivilegeKey);
        var costCenter = await RequireCostCenterAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(CostCenterWorkflow.BusinessObjectType, costCenter.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Cost center {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(costCenter, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(costCenter, actor, cancellationToken);

        return ToDto(costCenter);
    }

    private async Task ApproveInternalAsync(CostCenter costCenter, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = costCenter.Status;
        costCenter.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(costCenter.Id), actor,
            $"Cost center '{costCenter.CostCenterCode}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(costCenter.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(CostCenter costCenter, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = costCenter.Status;
        costCenter.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(costCenter.Id), actor,
            $"Cost center '{costCenter.CostCenterCode}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(costCenter.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid costCenterId) => new(costCenterId, AuditTargetType, "Self");

    private async Task<CostCenter> RequireCostCenterAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Cost center {id} was not found.");

    private static CostCenterDto ToDto(CostCenter c) => new(
        c.Id, c.DocumentNumber, c.Status.ToString(), c.CostCenterCode, c.CostCenterName, c.CostCenterNameArabic,
        c.ParentCostCenterId, c.IsPostable, c.IsActive, c.CreatedAt, c.CreatedBy);
}
