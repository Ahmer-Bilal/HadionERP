using System.Text.Json;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Finance.Application;

public sealed class BudgetService
{
    public const string NumberRangeKey = "FIN-BUD";

    private const string AuditTargetType = "Budget";
    private const string AuditSource = "Modules.Finance";

    private readonly IBudgetRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly ICostCenterLookup _costCenterLookup;

    public BudgetService(
        IBudgetRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        ICostCenterLookup costCenterLookup)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _costCenterLookup = costCenterLookup;
    }

    /// <summary>Creates a Draft budget. Rejects a Cost Center/fiscal year combination that already has a
    /// budget on file (any status) — a second one would leave <c>RealBudgetCheckService</c> with two
    /// candidates and no principled way to pick between them once both are Approved. A rejected budget's
    /// Cost Center/fiscal year is freed up again the moment it's rejected (see <see cref="RejectInternalAsync"/>
    /// — nothing here needs to change for that, the uniqueness check simply stops finding a blocking row).
    /// </summary>
    public async Task<BudgetDto> CreateAsync(
        CreateBudgetRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BudgetSecurity.MaintainPrivilegeKey);

        var costCenter = await _costCenterLookup.GetAsync(request.CostCenterId, cancellationToken)
            ?? throw new ArgumentException($"Cost center {request.CostCenterId} was not found.");
        if (!costCenter.IsActive)
            throw new ArgumentException($"Cost center '{costCenter.CostCenterCode}' is not active.");

        var existing = await _repository.GetActiveByCostCenterAndYearAsync(request.CostCenterId, request.FiscalYear, cancellationToken);
        if (existing is not null)
            throw new ArgumentException(
                $"Cost center '{costCenter.CostCenterCode}' already has a budget on file for fiscal year {request.FiscalYear} " +
                $"({existing.Status}). Reject it first if it needs to be replaced.");

        var budget = new Budget(actor, request.CostCenterId, request.FiscalYear, request.Amount);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        budget.AssignNumber(documentNumber);

        _repository.Add(budget);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(budget.Id), actor,
            $"Budget '{budget.DocumentNumber}' created for cost center '{costCenter.CostCenterCode}', " +
            $"fiscal year {budget.FiscalYear}, amount {budget.Amount}.", AuditSource);

        return ToDto(budget);
    }

    public async Task<BudgetDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var budget = await _repository.GetAsync(id, cancellationToken);
        return budget is null ? null : ToDto(budget);
    }

    public async Task<(IReadOnlyList<BudgetDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<BudgetDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BudgetSecurity.MaintainPrivilegeKey);
        var budget = await RequireBudgetAsync(id, cancellationToken);
        var fromStatus = budget.Status;
        budget.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(budget.Id), actor,
            $"Budget '{budget.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(budget.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(BudgetWorkflow.BusinessObjectType, BudgetWorkflow.SubmitTransition, budget.Id);
        if (instance is null) { await ApproveInternalAsync(budget, actor, cancellationToken); return ToDto(budget); }

        _workflowInstanceRepository.Add(instance);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(budget, actor, cancellationToken);

        return ToDto(budget);
    }

    public Task<BudgetDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<BudgetDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<BudgetDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, BudgetSecurity.ApprovePrivilegeKey);
        var budget = await RequireBudgetAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(BudgetWorkflow.BusinessObjectType, budget.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Budget {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(budget, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(budget, actor, cancellationToken);

        return ToDto(budget);
    }

    private async Task ApproveInternalAsync(Budget budget, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = budget.Status;
        budget.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(budget.Id), actor,
            $"Budget '{budget.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(budget.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(Budget budget, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = budget.Status;
        budget.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(budget.Id), actor,
            $"Budget '{budget.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(budget.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid budgetId) => new(budgetId, AuditTargetType, "Self");

    private async Task<Budget> RequireBudgetAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Budget {id} was not found.");

    private static BudgetDto ToDto(Budget b) => new(
        b.Id, b.DocumentNumber, b.Status.ToString(), b.CostCenterId, b.FiscalYear, b.Amount, b.CreatedAt, b.CreatedBy);
}
