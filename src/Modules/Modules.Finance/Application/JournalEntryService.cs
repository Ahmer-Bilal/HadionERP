using System.Text.Json;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Finance.Application;

public sealed class JournalEntryService
{
    public const string NumberRangeKey = "FIN-JE";

    private const string AuditTargetType = "JournalEntry";
    private const string AuditSource = "Modules.Finance";

    private readonly IJournalEntryRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IGLAccountLookup _glAccountLookup;
    private readonly ICostCenterLookup _costCenterLookup;

    public JournalEntryService(
        IJournalEntryRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IGLAccountLookup glAccountLookup,
        ICostCenterLookup costCenterLookup)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _glAccountLookup = glAccountLookup;
        _costCenterLookup = costCenterLookup;
    }

    public async Task<JournalEntryDto> CreateAsync(
        CreateJournalEntryRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, JournalEntrySecurity.MaintainPrivilegeKey);

        if (request.Lines.Count == 0)
            throw new ArgumentException("A journal entry needs at least one line.");

        var entry = new JournalEntry(actor, request.PostingDate, request.Description);
        foreach (var line in request.Lines)
        {
            await ValidateLineReferencesAsync(line.GLAccountId, line.CostCenterId, cancellationToken);
            entry.AddLine(line.GLAccountId, line.CostCenterId, line.DebitAmount, line.CreditAmount, line.LineDescription);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        entry.AssignNumber(documentNumber);

        _repository.Add(entry);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(entry.Id), actor,
            $"Journal entry '{entry.DocumentNumber}' created ({entry.Lines.Count} lines, {entry.TotalDebits} total).", AuditSource);

        return ToDto(entry);
    }

    public async Task<JournalEntryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetAsync(id, cancellationToken);
        return entry is null ? null : ToDto(entry);
    }

    public async Task<(IReadOnlyList<JournalEntryDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<JournalEntryDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, JournalEntrySecurity.MaintainPrivilegeKey);
        var entry = await RequireEntryAsync(id, cancellationToken);
        if (!entry.IsBalanced)
            throw new ArgumentException(
                $"Journal entry does not balance (debits {entry.TotalDebits}, credits {entry.TotalCredits}) and cannot be submitted.");

        var fromStatus = entry.Status;
        entry.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(entry.Id), actor,
            $"Journal entry '{entry.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(entry.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(JournalEntryWorkflow.BusinessObjectType, JournalEntryWorkflow.SubmitTransition, entry.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(entry, actor, cancellationToken);
        }

        return ToDto(entry);
    }

    public Task<JournalEntryDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<JournalEntryDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<JournalEntryDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, JournalEntrySecurity.ApprovePrivilegeKey);
        var entry = await RequireEntryAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(JournalEntryWorkflow.BusinessObjectType, entry.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(entry, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(entry, actor, cancellationToken);

        return ToDto(entry);
    }

    /// <summary>Posts an Approved entry — the point at which it has a real financial effect. A distinct
    /// action from Approve/reject a real batch-posting process, or a period-end close, might drive this
    /// separately from the approval decision itself, so the two stay separate endpoints even though both
    /// require the same Approver privilege.</summary>
    public async Task<JournalEntryDto> PostAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, JournalEntrySecurity.ApprovePrivilegeKey);
        var entry = await RequireEntryAsync(id, cancellationToken);
        var fromStatus = entry.Status;
        entry.Post(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(entry.Id), actor,
            $"Journal entry '{entry.DocumentNumber}' posted.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(entry.Status.ToString()), AuditSource);

        return ToDto(entry);
    }

    /// <summary>
    /// Reverses a Posted entry: marks the original as Reversed (audit trail — the original is never
    /// edited or deleted) and creates a brand-new mirror entry with every line's debit/credit swapped, so
    /// the ledger's actual balance is undone, not just the original's status. The mirror is driven straight
    /// through Submit → Approve → Post within this one call, bypassing the human approval workflow — its
    /// content is mechanically derived from an already-approved-and-posted document, not new judgment, the
    /// same reasoning SAP's FB08 "reverse document" uses. Requires the same Approver privilege as posting.
    /// </summary>
    public async Task<JournalEntryDto> ReverseAsync(Guid id, string actor, DateOnly reversalDate, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, JournalEntrySecurity.ApprovePrivilegeKey);
        var original = await RequireEntryAsync(id, cancellationToken);

        var mirror = new JournalEntry(actor, reversalDate, $"Reversal of {original.DocumentNumber}: {original.Description}");
        foreach (var line in original.Lines)
            mirror.AddLine(line.GLAccountId, line.CostCenterId, line.CreditAmount, line.DebitAmount, line.LineDescription);
        mirror.MarkAsReversalOf(original.Id);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, "C001", DateTimeOffset.UtcNow.Year);
        mirror.AssignNumber(documentNumber);
        mirror.Submit(actor);
        mirror.Approve(actor);
        mirror.Post(actor);

        _repository.Add(mirror);

        var fromStatus = original.Status;
        original.Reverse(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(original.Id), actor,
            $"Journal entry '{original.DocumentNumber}' reversed by '{mirror.DocumentNumber}'.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(original.Status.ToString()), AuditSource);
        _auditRecorder.RecordCreate(AuditReference(mirror.Id), actor,
            $"Journal entry '{mirror.DocumentNumber}' created as the reversal of '{original.DocumentNumber}'.", AuditSource);

        return ToDto(original);
    }

    private async Task ApproveInternalAsync(JournalEntry entry, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = entry.Status;
        entry.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(entry.Id), actor,
            $"Journal entry '{entry.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(entry.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(JournalEntry entry, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = entry.Status;
        entry.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(entry.Id), actor,
            $"Journal entry '{entry.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(entry.Status.ToString()), AuditSource);
    }

    /// <summary>Validates a line's G/L Account (and optional Cost Center) reference through
    /// Modules.MasterData.Contracts — never through MasterData's own Domain/Infrastructure — before the
    /// line is ever added to the entry. Both must exist, be Active, and be Postable (a header/grouping
    /// account or cost center can never receive a posting).</summary>
    private async Task ValidateLineReferencesAsync(Guid glAccountId, Guid? costCenterId, CancellationToken cancellationToken)
    {
        var account = await _glAccountLookup.GetAsync(glAccountId, cancellationToken)
            ?? throw new ArgumentException($"G/L account {glAccountId} was not found.");
        if (!account.IsActive)
            throw new ArgumentException($"G/L account '{account.AccountCode}' is not active.");
        if (!account.IsPostable)
            throw new ArgumentException($"G/L account '{account.AccountCode}' is a header/grouping account and cannot be posted to.");

        if (costCenterId is not { } id) return;
        var costCenter = await _costCenterLookup.GetAsync(id, cancellationToken)
            ?? throw new ArgumentException($"Cost center {id} was not found.");
        if (!costCenter.IsActive)
            throw new ArgumentException($"Cost center '{costCenter.CostCenterCode}' is not active.");
        if (!costCenter.IsPostable)
            throw new ArgumentException($"Cost center '{costCenter.CostCenterCode}' is a header/grouping cost center and cannot be posted to.");
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid entryId) => new(entryId, AuditTargetType, "Self");

    private async Task<JournalEntry> RequireEntryAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Journal entry {id} was not found.");

    private static JournalEntryDto ToDto(JournalEntry e) => new(
        e.Id, e.DocumentNumber, e.Status.ToString(), e.PostingDate, e.Description, e.ReversalOfEntryId,
        e.TotalDebits, e.TotalCredits, e.IsBalanced,
        e.Lines.Select(l => new JournalLineDto(l.Id, l.GLAccountId, l.CostCenterId, l.DebitAmount, l.CreditAmount, l.LineDescription)).ToList(),
        e.CreatedAt, e.CreatedBy);
}
