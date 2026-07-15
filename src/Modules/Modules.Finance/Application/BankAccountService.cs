using System.Text.Json;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Finance.Application;

public sealed class BankAccountService
{
    public const string NumberRangeKey = "FIN-BANK";

    private const string AuditTargetType = "BankAccount";
    private const string AuditSource = "Modules.Finance";

    private readonly IBankAccountRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IGLAccountLookup _glAccountLookup;

    public BankAccountService(
        IBankAccountRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IGLAccountLookup glAccountLookup)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _glAccountLookup = glAccountLookup;
    }

    public async Task<BankAccountDto> CreateAsync(
        CreateBankAccountRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BankAccountSecurity.MaintainPrivilegeKey);

        var existing = await _repository.GetByCodeAsync(request.AccountCode, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Bank account code '{request.AccountCode}' is already in use.");

        await ValidateLinkedGLAccountAsync(request.LinkedGLAccountId, cancellationToken);

        var bankAccount = new BankAccount(actor, request.AccountCode, request.AccountName, request.BankName, request.LinkedGLAccountId);
        bankAccount.UpdateAccountNameArabic(request.AccountNameArabic);
        bankAccount.UpdateIban(request.Iban);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        bankAccount.AssignNumber(documentNumber);

        _repository.Add(bankAccount);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(bankAccount.Id), actor,
            $"Bank account '{bankAccount.AccountCode}' ({bankAccount.AccountName}) created.", AuditSource);

        return ToDto(bankAccount);
    }

    public async Task<BankAccountDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bankAccount = await _repository.GetAsync(id, cancellationToken);
        return bankAccount is null ? null : ToDto(bankAccount);
    }

    public async Task<(IReadOnlyList<BankAccountDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<BankAccountDto> UpdateAsync(
        Guid id, UpdateBankAccountRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BankAccountSecurity.MaintainPrivilegeKey);
        var bankAccount = await RequireBankAccountAsync(id, cancellationToken);

        bankAccount.UpdateAccountName(request.AccountName);
        bankAccount.UpdateAccountNameArabic(request.AccountNameArabic);
        bankAccount.UpdateBankName(request.BankName);
        bankAccount.UpdateIban(request.Iban);
        if (request.IsActive) bankAccount.Activate(); else bankAccount.Deactivate();

        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(bankAccount.Id), actor,
            $"Bank account '{bankAccount.AccountCode}' updated.", Array.Empty<FieldValueChange>(), AuditSource);

        return ToDto(bankAccount);
    }

    public async Task<BankAccountDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BankAccountSecurity.MaintainPrivilegeKey);
        var bankAccount = await RequireBankAccountAsync(id, cancellationToken);
        var fromStatus = bankAccount.Status;
        bankAccount.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(bankAccount.Id), actor,
            $"Bank account '{bankAccount.AccountCode}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(bankAccount.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(BankAccountWorkflow.BusinessObjectType, BankAccountWorkflow.SubmitTransition, bankAccount.Id);
        if (instance is null) { await ApproveInternalAsync(bankAccount, actor, cancellationToken); return ToDto(bankAccount); }

        _workflowInstanceRepository.Add(instance);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(bankAccount, actor, cancellationToken);

        return ToDto(bankAccount);
    }

    public Task<BankAccountDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<BankAccountDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<BankAccountDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, BankAccountSecurity.ApprovePrivilegeKey);
        var bankAccount = await RequireBankAccountAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(BankAccountWorkflow.BusinessObjectType, bankAccount.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(bankAccount, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(bankAccount, actor, cancellationToken);

        return ToDto(bankAccount);
    }

    private async Task ApproveInternalAsync(BankAccount bankAccount, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = bankAccount.Status;
        bankAccount.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(bankAccount.Id), actor,
            $"Bank account '{bankAccount.AccountCode}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(bankAccount.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(BankAccount bankAccount, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = bankAccount.Status;
        bankAccount.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(bankAccount.Id), actor,
            $"Bank account '{bankAccount.AccountCode}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(bankAccount.Status.ToString()), AuditSource);
    }

    private async Task ValidateLinkedGLAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _glAccountLookup.GetAsync(accountId, cancellationToken)
            ?? throw new ArgumentException($"Linked G/L account {accountId} was not found.");
        if (!account.IsActive) throw new ArgumentException($"Linked G/L account '{account.AccountCode}' is not active.");
        if (!account.IsPostable) throw new ArgumentException($"Linked G/L account '{account.AccountCode}' is a header/grouping account and cannot be posted to.");
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid bankAccountId) => new(bankAccountId, AuditTargetType, "Self");

    private async Task<BankAccount> RequireBankAccountAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Bank account {id} was not found.");

    private static BankAccountDto ToDto(BankAccount b) => new(
        b.Id, b.DocumentNumber, b.Status.ToString(), b.AccountCode, b.AccountName, b.AccountNameArabic,
        b.BankName, b.Iban, b.LinkedGLAccountId, b.IsActive, b.CreatedAt, b.CreatedBy);
}
