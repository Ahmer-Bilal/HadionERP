using Modules.Finance.Domain;
using Modules.Identity.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Security;

namespace Modules.Finance.Application;

/// <summary>
/// Application service for the Period Closing Center's real per-person checklist
/// (`UI/Finance/d1f20165-...png`). Generates the ten fixed <see cref="ClosingActivityCatalog"/> activities
/// once per <see cref="FiscalPeriod"/> and keeps the three auto-tracked ones (Accounts Payable/Receivable,
/// Journal Review) in sync with real document status every time the checklist is read — see
/// <see cref="ClosingActivityStep"/>'s own doc comment for the full reasoning.
/// </summary>
public sealed class ClosingActivityService
{
    private const string AuditTargetType = "ClosingActivity";
    private const string AuditSource = "Modules.Finance";

    private readonly IClosingActivityRepository _repository;
    private readonly IFiscalYearRepository _fiscalYearRepository;
    private readonly IAPInvoiceRepository _apInvoiceRepository;
    private readonly IARInvoiceRepository _arInvoiceRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IUserLookup _userLookup;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IAuditLog _auditLog;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;

    public ClosingActivityService(
        IClosingActivityRepository repository,
        IFiscalYearRepository fiscalYearRepository,
        IAPInvoiceRepository apInvoiceRepository,
        IARInvoiceRepository arInvoiceRepository,
        IJournalEntryRepository journalEntryRepository,
        IBankAccountRepository bankAccountRepository,
        IUserLookup userLookup,
        IAuditRecorder auditRecorder,
        IAuditLog auditLog,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore)
    {
        _repository = repository;
        _fiscalYearRepository = fiscalYearRepository;
        _apInvoiceRepository = apInvoiceRepository;
        _arInvoiceRepository = arInvoiceRepository;
        _journalEntryRepository = journalEntryRepository;
        _bankAccountRepository = bankAccountRepository;
        _userLookup = userLookup;
        _auditRecorder = auditRecorder;
        _auditLog = auditLog;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
    }

    /// <summary>The checklist for one period — generates it on first call, refreshes the three auto-tracked
    /// activities against live document status on every call. Never returns a stale view: an AP Invoice
    /// posted a minute ago already shows as closed the next time this is fetched.</summary>
    public async Task<IReadOnlyList<ClosingActivityDto>> ListForPeriodAsync(
        Guid fiscalYearId, int periodNumber, string actor, CancellationToken cancellationToken = default)
    {
        var period = await RequirePeriodAsync(fiscalYearId, periodNumber, cancellationToken);
        var activities = await _repository.ListForPeriodAsync(period.Id, cancellationToken);

        if (activities.Count == 0)
        {
            activities = await GenerateAllAsync(period, cancellationToken);
        }
        else
        {
            await RefreshAutoTrackedAsync(activities, period, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var userIds = activities.Where(a => a.AssignedToUserId.HasValue).Select(a => a.AssignedToUserId!.Value).Distinct();
        var users = new Dictionary<Guid, UserSummary>();
        foreach (var id in userIds)
        {
            var user = await _userLookup.GetAsync(id, cancellationToken);
            if (user is not null) users[id] = user;
        }

        return activities.OrderBy(a => a.SequenceNumber).Select(a => ToDto(a, users)).ToList();
    }

    public async Task<ClosingActivityDto> AssignAsync(
        Guid activityId, AssignClosingActivityRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAdministerAuthorization(actor);
        var activity = await RequireActivityAsync(activityId, cancellationToken);

        var user = await _userLookup.GetAsync(request.UserId, cancellationToken)
            ?? throw new ArgumentException($"User {request.UserId} was not found.");
        if (!user.IsActive)
            throw new ArgumentException($"User '{user.Username}' is not active.");

        activity.Assign(request.UserId, request.DueDate, actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(activity.Id), actor,
            $"Closing activity '{activity.ActivityKey}' assigned to '{user.Username}'.", Array.Empty<FieldValueChange>(), AuditSource);

        var users = new Dictionary<Guid, UserSummary> { [user.Id] = user };
        return ToDto(activity, users);
    }

    /// <summary>Manual step toggle — rejects an auto-tracked step outright (see
    /// <see cref="ClosingActivityStep"/>'s own doc comment: those only ever change via
    /// <see cref="ListForPeriodAsync"/>'s live-status refresh). Gated to the activity's own assignee or a
    /// Finance Manager — the actual mechanism behind "every person has its own duties."</summary>
    public async Task<ClosingActivityDto> ToggleStepAsync(
        Guid activityId, Guid stepId, bool isCompleted, string actor, CancellationToken cancellationToken = default)
    {
        var activity = await RequireActivityAsync(activityId, cancellationToken);
        await RequireOwnerOrAdministerAsync(actor, activity, cancellationToken);

        var step = activity.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new KeyNotFoundException($"Step {stepId} was not found on closing activity {activityId}.");
        if (step.IsAutoTracked)
            throw new InvalidOperationException(
                $"'{step.Description}' tracks a real document's own status and can't be toggled by hand.");

        step.SetCompleted(isCompleted, actor);
        activity.RefreshStatus(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(activity.Id), actor,
            $"Closing activity '{activity.ActivityKey}' step '{step.Description}' marked {(isCompleted ? "complete" : "incomplete")}.",
            Array.Empty<FieldValueChange>(), AuditSource);

        return await ToDtoWithUserAsync(activity, cancellationToken);
    }

    public async Task<ClosingActivityDto> SetBlockedAsync(
        Guid activityId, bool isBlocked, string actor, CancellationToken cancellationToken = default)
    {
        var activity = await RequireActivityAsync(activityId, cancellationToken);
        await RequireOwnerOrAdministerAsync(actor, activity, cancellationToken);

        activity.SetBlocked(isBlocked, actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(activity.Id), actor,
            $"Closing activity '{activity.ActivityKey}' {(isBlocked ? "marked blocked" : "unblocked")}.",
            Array.Empty<FieldValueChange>(), AuditSource);

        return await ToDtoWithUserAsync(activity, cancellationToken);
    }

    /// <summary>Real, rule-based insights over actual checklist state — no AI call, but every message is
    /// derived from real data, never fabricated (see the discussion that settled on this approach instead
    /// of skipping the mockup's "Closing Insights" panel or faking an AI response).</summary>
    public async Task<IReadOnlyList<ClosingInsightDto>> GetInsightsAsync(
        Guid fiscalYearId, int periodNumber, string actor, CancellationToken cancellationToken = default)
    {
        var period = await RequirePeriodAsync(fiscalYearId, periodNumber, cancellationToken);
        var activities = await _repository.ListForPeriodAsync(period.Id, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var insights = new List<ClosingInsightDto>();

        var blocked = activities.Where(a => a.Status == ClosingActivityStatus.Blocked).ToList();
        var overdue = activities.Where(a => a.Status != ClosingActivityStatus.Completed && a.DueDate is { } due && due < today).ToList();
        var atRisk = blocked.Concat(overdue).Select(a => ClosingActivityCatalog.Get(a.ActivityKey).Title).Distinct().ToList();

        if (atRisk.Count > 0)
            insights.Add(new ClosingInsightDto("AttentionRequired", "Attention Required",
                $"{string.Join(" and ", atRisk)} need attention to meet the target date."));
        else
            insights.Add(new ClosingInsightDto("OnTrack", "On Track",
                $"You are on track to close the period by {period.TargetCloseDate:dd-MMM-yyyy}."));

        insights.Add(new ClosingInsightDto("BestPractice", "Best Practice",
            "Complete AP & AR before Inventory for smoother closing."));

        return insights;
    }

    public async Task<IReadOnlyList<ClosingActivityLogEntryDto>> GetActivityLogAsync(
        Guid fiscalYearId, int periodNumber, int take, string actor, CancellationToken cancellationToken = default)
    {
        var period = await RequirePeriodAsync(fiscalYearId, periodNumber, cancellationToken);
        var activities = await _repository.ListForPeriodAsync(period.Id, cancellationToken);

        var entries = activities
            .SelectMany(a => _auditLog.GetFor(new BusinessObjectReference(a.Id, AuditTargetType, "Self")))
            .OrderByDescending(e => e.OccurredAt)
            .Take(take)
            .Select(e => new ClosingActivityLogEntryDto(e.OccurredAt, e.ActorPrincipalKey, e.Summary))
            .ToList();

        return entries;
    }

    /// <summary>Reconstructed from each step's own real <see cref="ClosingActivityStep.CompletedAt"/>
    /// timestamp — not a fabricated smooth curve. A brand-new period with little real usage yet will show a
    /// sparse or flat line; that's honest, not a bug (see the discussion that settled on this approach).
    /// </summary>
    public async Task<IReadOnlyList<CompletionTrendPointDto>> GetCompletionTrendAsync(
        Guid fiscalYearId, int periodNumber, string actor, CancellationToken cancellationToken = default)
    {
        var period = await RequirePeriodAsync(fiscalYearId, periodNumber, cancellationToken);
        var activities = await _repository.ListForPeriodAsync(period.Id, cancellationToken);
        var allSteps = activities.SelectMany(a => a.Steps).ToList();
        var totalSteps = allSteps.Count;
        if (totalSteps == 0) return Array.Empty<CompletionTrendPointDto>();

        var completionDates = allSteps.Where(s => s.CompletedAt.HasValue)
            .Select(s => DateOnly.FromDateTime(s.CompletedAt!.Value.UtcDateTime))
            .OrderBy(d => d)
            .ToList();
        if (completionDates.Count == 0) return Array.Empty<CompletionTrendPointDto>();

        var points = new List<CompletionTrendPointDto>();
        var runningCompleted = 0;
        foreach (var day in completionDates.Distinct())
        {
            runningCompleted += completionDates.Count(d => d == day);
            points.Add(new CompletionTrendPointDto(day, Math.Round(100m * runningCompleted / totalSteps, 1)));
        }
        return points;
    }

    private async Task<IReadOnlyList<ClosingActivity>> GenerateAllAsync(FiscalPeriod period, CancellationToken cancellationToken)
    {
        var generated = new List<ClosingActivity>();
        foreach (var definition in ClosingActivityCatalog.All)
        {
            var activity = new ClosingActivity(period.Id, definition.Key, definition.SequenceNumber);
            await PopulateStepsAsync(activity, definition.Key, period, cancellationToken);
            activity.RefreshStatus("system/auto-tracked");
            _repository.Add(activity);
            generated.Add(activity);
        }
        return generated;
    }

    private async Task PopulateStepsAsync(ClosingActivity activity, string activityKey, FiscalPeriod period, CancellationToken cancellationToken)
    {
        switch (activityKey)
        {
            case ClosingActivityCatalog.BankReconciliation:
                var accounts = await _bankAccountRepository.ListActiveAsync(cancellationToken);
                foreach (var account in accounts)
                    activity.AddStep($"Reconcile account '{account.AccountCode} — {account.AccountName}'", null, null);
                break;

            case ClosingActivityCatalog.AccountsPayable:
                var apInvoices = await _apInvoiceRepository.ListByInvoiceDateRangeAsync(period.StartDate, period.EndDate, cancellationToken);
                foreach (var invoice in apInvoices.Where(IsPendingClosure))
                    activity.AddStep($"Close AP Invoice '{invoice.DocumentNumber}' ({invoice.VendorInvoiceNumber})", "APInvoice", invoice.Id);
                break;

            case ClosingActivityCatalog.AccountsReceivable:
                var arInvoices = await _arInvoiceRepository.ListByInvoiceDateRangeAsync(period.StartDate, period.EndDate, cancellationToken);
                foreach (var invoice in arInvoices.Where(IsPendingClosure))
                    activity.AddStep($"Close AR Invoice '{invoice.DocumentNumber}' ({invoice.CustomerReference})", "ARInvoice", invoice.Id);
                break;

            case ClosingActivityCatalog.JournalReview:
                var entries = await _journalEntryRepository.ListManualByPostingDateRangeAsync(period.StartDate, period.EndDate, cancellationToken);
                foreach (var entry in entries.Where(IsPendingClosure))
                    activity.AddStep($"Review Journal Entry '{entry.DocumentNumber}'", "JournalEntry", entry.Id);
                break;

            default:
                var title = ClosingActivityCatalog.Get(activityKey).Title;
                activity.AddStep($"Confirm {title} complete for this period", null, null);
                break;
        }
    }

    private async Task RefreshAutoTrackedAsync(IReadOnlyList<ClosingActivity> activities, FiscalPeriod period, CancellationToken cancellationToken)
    {
        foreach (var activity in activities)
        {
            if (!ClosingActivityCatalog.AutoTrackedKeys.Contains(activity.ActivityKey)) continue;

            var pendingIds = activity.ActivityKey switch
            {
                ClosingActivityCatalog.AccountsPayable =>
                    (await _apInvoiceRepository.ListByInvoiceDateRangeAsync(period.StartDate, period.EndDate, cancellationToken))
                        .Where(IsPendingClosure).Select(i => i.Id).ToHashSet(),
                ClosingActivityCatalog.AccountsReceivable =>
                    (await _arInvoiceRepository.ListByInvoiceDateRangeAsync(period.StartDate, period.EndDate, cancellationToken))
                        .Where(IsPendingClosure).Select(i => i.Id).ToHashSet(),
                ClosingActivityCatalog.JournalReview =>
                    (await _journalEntryRepository.ListManualByPostingDateRangeAsync(period.StartDate, period.EndDate, cancellationToken))
                        .Where(IsPendingClosure).Select(e => e.Id).ToHashSet(),
                _ => new HashSet<Guid>(),
            };

            foreach (var step in activity.Steps.Where(s => s.IsAutoTracked))
            {
                var stillPending = pendingIds.Contains(step.LinkedDocumentId!.Value);
                step.SetCompletionFromLiveStatus(isCompleted: !stillPending);
            }

            // New documents that entered the period since the checklist was first generated — additive
            // refresh, never removing an already-generated step even if its document later falls outside
            // range (a real audit trail of "this needed review" shouldn't just disappear).
            var alreadyTracked = activity.Steps.Where(s => s.IsAutoTracked).Select(s => s.LinkedDocumentId!.Value).ToHashSet();
            var newIds = pendingIds.Except(alreadyTracked);
            foreach (var newId in newIds)
                await AddNewAutoStepAsync(activity, activity.ActivityKey, newId, cancellationToken);

            activity.RefreshStatus("system/auto-tracked");
        }
    }

    private async Task AddNewAutoStepAsync(ClosingActivity activity, string activityKey, Guid documentId, CancellationToken cancellationToken)
    {
        switch (activityKey)
        {
            case ClosingActivityCatalog.AccountsPayable:
                var ap = await _apInvoiceRepository.GetAsync(documentId, cancellationToken);
                if (ap is not null) activity.AddStep($"Close AP Invoice '{ap.DocumentNumber}' ({ap.VendorInvoiceNumber})", "APInvoice", ap.Id);
                break;
            case ClosingActivityCatalog.AccountsReceivable:
                var ar = await _arInvoiceRepository.GetAsync(documentId, cancellationToken);
                if (ar is not null) activity.AddStep($"Close AR Invoice '{ar.DocumentNumber}' ({ar.CustomerReference})", "ARInvoice", ar.Id);
                break;
            case ClosingActivityCatalog.JournalReview:
                var je = await _journalEntryRepository.GetAsync(documentId, cancellationToken);
                if (je is not null) activity.AddStep($"Review Journal Entry '{je.DocumentNumber}'", "JournalEntry", je.Id);
                break;
        }
    }

    private static bool IsPendingClosure(APInvoice invoice) => IsPendingClosure(invoice.Status);
    private static bool IsPendingClosure(ARInvoice invoice) => IsPendingClosure(invoice.Status);
    private static bool IsPendingClosure(JournalEntry entry) => IsPendingClosure(entry.Status);

    private static bool IsPendingClosure(BusinessObjectStatus status) =>
        status is BusinessObjectStatus.Draft or BusinessObjectStatus.Submitted or BusinessObjectStatus.InApproval or BusinessObjectStatus.Approved;

    private async Task<FiscalPeriod> RequirePeriodAsync(Guid fiscalYearId, int periodNumber, CancellationToken cancellationToken)
    {
        var fiscalYear = await _fiscalYearRepository.GetAsync(fiscalYearId, cancellationToken)
            ?? throw new KeyNotFoundException($"Fiscal year {fiscalYearId} was not found.");
        return fiscalYear.Periods.FirstOrDefault(p => p.PeriodNumber == periodNumber)
            ?? throw new ArgumentException($"Fiscal year {fiscalYear.Year} has no period {periodNumber}.");
    }

    private async Task<ClosingActivity> RequireActivityAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Closing activity {id} was not found.");

    private async Task<ClosingActivityDto> ToDtoWithUserAsync(ClosingActivity activity, CancellationToken cancellationToken)
    {
        var users = new Dictionary<Guid, UserSummary>();
        if (activity.AssignedToUserId is { } userId)
        {
            var user = await _userLookup.GetAsync(userId, cancellationToken);
            if (user is not null) users[userId] = user;
        }
        return ToDto(activity, users);
    }

    private void RequireAdministerAuthorization(string actor)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), FiscalYearSecurity.AdministerPrivilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private async Task RequireOwnerOrAdministerAsync(string actor, ClosingActivity activity, CancellationToken cancellationToken)
    {
        var adminResult = _authorizationService.Authorize(BuildPrincipal(actor), FiscalYearSecurity.AdministerPrivilegeKey);
        if (adminResult.Allowed) return;

        if (activity.AssignedToUserId is { } userId)
        {
            var user = await _userLookup.GetAsync(userId, cancellationToken);
            if (user is not null && string.Equals(user.Username, actor, StringComparison.Ordinal)) return;
        }

        throw new UnauthorizedAccessException(
            $"Only the person '{activity.ActivityKey}' is assigned to, or a Finance Manager, may update it.");
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid activityId) => new(activityId, AuditTargetType, "Self");

    private static ClosingActivityDto ToDto(ClosingActivity a, IReadOnlyDictionary<Guid, UserSummary> users)
    {
        var definition = ClosingActivityCatalog.Get(a.ActivityKey);
        UserSummary? assignedUser = a.AssignedToUserId is { } id && users.TryGetValue(id, out var u) ? u : null;
        return new ClosingActivityDto(
            a.Id, a.FiscalPeriodId, a.ActivityKey, a.SequenceNumber, definition.Title, definition.Description,
            a.AssignedToUserId, assignedUser?.DisplayName, assignedUser?.RoleKeys.FirstOrDefault(),
            a.DueDate, a.Status.ToString(),
            a.Steps.Count(s => s.IsCompleted), a.Steps.Count,
            a.Steps.Select(s => new ClosingActivityStepDto(s.Id, s.Description, s.IsAutoTracked, s.IsCompleted, s.CompletedBy, s.CompletedAt)).ToList(),
            a.LastActionBy, a.LastActionAt);
    }
}
