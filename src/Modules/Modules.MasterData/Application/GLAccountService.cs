using System.Text.Json;
using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.MasterData.Application;

public sealed class GLAccountService
{
    public const string NumberRangeKey = "MD-GL";

    private const string AuditTargetType = "GLAccount";
    private const string AuditSource = "Modules.MasterData";

    private readonly IGLAccountRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;

    public GLAccountService(
        IGLAccountRepository repository,
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

    public async Task<GLAccountDto> CreateAsync(
        CreateGLAccountRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, GLAccountSecurity.MaintainPrivilegeKey);

        if (!Enum.TryParse<AccountType>(request.AccountType, ignoreCase: true, out var accountType))
            throw new ArgumentException($"Invalid account type '{request.AccountType}'. Expected Asset, Liability, Equity, Revenue, or Expense.");

        var existing = await _repository.GetByCodeAsync(request.AccountCode, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Account code '{request.AccountCode}' is already in use.");

        var account = new GLAccount(actor, request.AccountCode, request.AccountName, accountType);
        account.UpdateAccountNameArabic(request.AccountNameArabic);
        account.AssignParent(request.ParentAccountId);
        if (!request.IsPostable) account.SetPostable(false);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        account.AssignNumber(documentNumber);

        _repository.Add(account);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(account.Id), actor,
            $"G/L account '{account.AccountCode}' ({account.AccountName}) created.", AuditSource);

        return ToDto(account);
    }

    public async Task<GLAccountDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAsync(id, cancellationToken);
        return account is null ? null : ToDto(account);
    }

    public async Task<(IReadOnlyList<GLAccountDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<GLAccountDto> UpdateAsync(
        Guid id, UpdateGLAccountRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, GLAccountSecurity.MaintainPrivilegeKey);
        var account = await RequireAccountAsync(id, cancellationToken);

        account.UpdateAccountName(request.AccountName);
        account.UpdateAccountNameArabic(request.AccountNameArabic);
        account.AssignParent(request.ParentAccountId);
        account.SetPostable(request.IsPostable);
        if (request.IsActive) account.Activate(); else account.Deactivate();

        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(account.Id), actor,
            $"G/L account '{account.AccountCode}' updated.",
            new[]
            {
                new FieldValueChange("AccountName", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.AccountName)),
                new FieldValueChange("IsPostable", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.IsPostable)),
                new FieldValueChange("IsActive", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.IsActive)),
            },
            AuditSource);

        return ToDto(account);
    }

    public async Task<GLAccountDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, GLAccountSecurity.MaintainPrivilegeKey);
        var account = await RequireAccountAsync(id, cancellationToken);
        var fromStatus = account.Status;
        account.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(account.Id), actor,
            $"G/L account '{account.AccountCode}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(account.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(GLAccountWorkflow.BusinessObjectType, GLAccountWorkflow.SubmitTransition, account.Id);
        if (instance is null) { await ApproveInternalAsync(account, actor, cancellationToken); return ToDto(account); }

        _workflowInstanceRepository.Add(instance);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(account, actor, cancellationToken);

        return ToDto(account);
    }

    public Task<GLAccountDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<GLAccountDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<GLAccountDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, GLAccountSecurity.ApprovePrivilegeKey);
        var account = await RequireAccountAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(GLAccountWorkflow.BusinessObjectType, account.Id, cancellationToken)
            ?? throw new InvalidOperationException($"G/L account {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(account, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(account, actor, cancellationToken);

        return ToDto(account);
    }

    private async Task ApproveInternalAsync(GLAccount account, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = account.Status;
        account.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(account.Id), actor,
            $"G/L account '{account.AccountCode}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(account.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(GLAccount account, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = account.Status;
        account.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(account.Id), actor,
            $"G/L account '{account.AccountCode}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(account.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid accountId) => new(accountId, AuditTargetType, "Self");

    private async Task<GLAccount> RequireAccountAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"G/L account {id} was not found.");

    /// <summary>Deletes a G/L account outright — approval-gated (requires <see cref="GLAccountSecurity.ApprovePrivilegeKey"/>,
    /// not just Maintain) so a Maintainer alone can never delete something they created; the same
    /// second-person control this codebase already enforces for Approve/Reject, applied to deletion too,
    /// rather than a separate ad hoc "delete workflow." Only ever allowed while the account
    /// <see cref="Platform.Core.BusinessObject.CanHardDelete"/> — Draft, never approved — matching this
    /// platform's "correct by reversal, not by deletion" rule for anything that ever had a real effect;
    /// <see cref="UpdateAsync"/>'s <c>IsActive</c> toggle (Deactivate) is the equivalent action once an
    /// account is Approved.</summary>
    public async Task DeleteAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, GLAccountSecurity.ApprovePrivilegeKey);
        var account = await RequireAccountAsync(id, cancellationToken);

        if (!account.CanHardDelete)
            throw new InvalidOperationException(
                $"G/L account '{account.AccountCode}' has already been submitted for approval and can no longer be deleted — deactivate it instead.");

        _repository.Remove(account);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordDeleteAttempt(AuditReference(account.Id), actor,
            $"G/L account '{account.AccountCode}' ({account.AccountName}) deleted.", AuditSource);
    }

    private static GLAccountDto ToDto(GLAccount a) => new(
        a.Id, a.DocumentNumber, a.Status.ToString(), a.AccountCode, a.AccountName, a.AccountNameArabic,
        a.AccountType.ToString(), a.NormalBalance, a.ParentAccountId, a.IsPostable, a.IsActive, a.CreatedAt, a.CreatedBy,
        a.ModifiedAt, a.ModifiedBy);
}
