namespace Modules.Finance.Domain;

/// <summary>
/// One row of a <see cref="FiscalPeriod"/>'s closing checklist — the real per-person "duties" workflow
/// `UI/Finance/d1f20165-...png` (Period Closing Center) shows: ten fixed activities, each with one
/// responsible person, a status, and its own sub-checklist of <see cref="Steps"/>. Generated once per period
/// (all ten, per <see cref="ClosingActivityCatalog"/>) by <c>ClosingActivityService</c>, the same
/// "mechanically derived, not user-entered" reasoning <see cref="FiscalYear"/> uses for its own 12 periods.
///
/// Not a <see cref="Platform.Core.BusinessObject"/> — same reasoning as <see cref="FiscalYear"/>/
/// <see cref="Budget"/>'s admin-configured-control-data shape, except the gating here is per-instance
/// ownership rather than a single administrative role: only <see cref="AssignedToUserId"/> (or someone
/// holding <c>FiscalYearSecurity.AdministerPrivilegeKey</c>) may change this activity's own state — the
/// literal mechanism behind "every person has its own duties."
/// </summary>
public sealed class ClosingActivity
{
    private readonly List<ClosingActivityStep> _steps = new();

    public Guid Id { get; private set; }

    public Guid FiscalPeriodId { get; private set; }

    /// <summary>One of <see cref="ClosingActivityCatalog"/>'s ten fixed keys.</summary>
    public string ActivityKey { get; private set; }

    public int SequenceNumber { get; private set; }

    /// <summary>Null until a Finance Manager assigns it — an unassigned activity has no one whose "duty" it
    /// is yet, matching real closing practice (the checklist exists before ownership is handed out).</summary>
    public Guid? AssignedToUserId { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public ClosingActivityStatus Status { get; private set; }

    public IReadOnlyCollection<ClosingActivityStep> Steps => _steps.AsReadOnly();

    public string? LastActionBy { get; private set; }

    public DateTimeOffset? LastActionAt { get; private set; }

    public ClosingActivity(Guid fiscalPeriodId, string activityKey, int sequenceNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityKey);
        Id = Guid.NewGuid();
        FiscalPeriodId = fiscalPeriodId;
        ActivityKey = activityKey;
        SequenceNumber = sequenceNumber;
        Status = ClosingActivityStatus.NotStarted;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private ClosingActivity()
    {
        ActivityKey = null!;
    }

    public ClosingActivityStep AddStep(string description, string? linkedDocumentType, Guid? linkedDocumentId)
    {
        var step = new ClosingActivityStep(description, linkedDocumentType, linkedDocumentId);
        _steps.Add(step);
        return step;
    }

    public void Assign(Guid userId, DateOnly? dueDate, string actor)
    {
        AssignedToUserId = userId;
        DueDate = dueDate;
        LastActionBy = actor;
        LastActionAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Explicit Blocked toggle — see this class's own doc comment for why Blocked is never
    /// auto-derived from step completion the way NotStarted/InProgress/Completed are.</summary>
    public void SetBlocked(bool isBlocked, string actor)
    {
        Status = isBlocked ? ClosingActivityStatus.Blocked : RecomputeStatusFromSteps();
        LastActionBy = actor;
        LastActionAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Re-derives <see cref="Status"/> from <see cref="Steps"/> completion — called by
    /// <c>ClosingActivityService</c> after any step changes (manual toggle or auto-tracked live-status
    /// refresh). A no-op while <see cref="Status"/> is <see cref="ClosingActivityStatus.Blocked"/>: an
    /// explicit unblock (<see cref="SetBlocked"/> with <c>false</c>) is required first, the same "still
    /// blocked on an external dependency even after doing some prep work" reasoning that class's doc
    /// comment gives.</summary>
    public void RefreshStatus(string actor)
    {
        if (Status == ClosingActivityStatus.Blocked) return;
        var newStatus = RecomputeStatusFromSteps();
        if (newStatus == Status) return;
        Status = newStatus;
        LastActionBy = actor;
        LastActionAt = DateTimeOffset.UtcNow;
    }

    private ClosingActivityStatus RecomputeStatusFromSteps()
    {
        // Zero steps reads as Completed, not NotStarted: for an auto-tracked activity it means "nothing
        // pending this period" (trivially satisfied), and no manual activity generates zero steps under
        // normal conditions (BankReconciliation with zero active bank accounts is the one real edge case,
        // and "nothing to reconcile" is Completed under the same reasoning).
        if (_steps.Count == 0) return ClosingActivityStatus.Completed;
        var completed = _steps.Count(s => s.IsCompleted);
        if (completed == 0) return ClosingActivityStatus.NotStarted;
        return completed == _steps.Count ? ClosingActivityStatus.Completed : ClosingActivityStatus.InProgress;
    }
}
