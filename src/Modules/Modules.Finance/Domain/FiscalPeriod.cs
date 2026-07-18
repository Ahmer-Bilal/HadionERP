namespace Modules.Finance.Domain;

/// <summary>
/// One calendar month within a <see cref="FiscalYear"/> — the actual granularity real period-close control
/// operates at (SAP's own fiscal year variant doesn't have an open/closed flag on the year itself, only on
/// its periods). Exists only through its parent <see cref="FiscalYear"/>, the same "0..n child collection,
/// only exists through the aggregate root" pattern as <see cref="JournalLine"/> within <see cref="JournalEntry"/>.
///
/// Deliberately no Draft/Submit/Approve lifecycle — see <see cref="FiscalYear"/>'s own doc comment for why
/// opening/closing a period is an immediate-effect, single-privilege-gated action (the same reasoning
/// <c>Modules.MasterData.Domain.LookupValue.Activate</c>/<c>Deactivate</c> already established in this
/// codebase), not a workflow-approved document.
/// </summary>
public sealed class FiscalPeriod
{
    public Guid Id { get; private set; }

    /// <summary>1–12, matching calendar months (January = 1) within the owning <see cref="FiscalYear"/>.
    /// This platform only supports a calendar-year fiscal year for now (see <see cref="FiscalYear"/>'s own
    /// doc comment) — a custom fiscal-year-start-month/13-period variant is real, disclosed future depth,
    /// not built here.</summary>
    public int PeriodNumber { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    /// <summary>True while new postings dated within this period are allowed. False once a Finance
    /// Controller has closed the period — <c>JournalEntryService</c> checks this before ever posting a real
    /// ledger effect, the one thing this entire entity exists to gate (per
    /// `UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md`'s Period Closing Center entry: "the ability to block
    /// postings into a locked period").</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The date closing activities for this period are targeted to finish by — matches the
    /// mockup's "Target Close Date / Edit Close Schedule" card. Distinct from <see cref="EndDate"/> (the
    /// calendar period itself): real closing work happens *after* a period ends, not during it. Defaults to
    /// five business-adjacent days after <see cref="EndDate"/> (SAP shops commonly target "close within the
    /// first week of the next month"), editable afterward via <c>FiscalYearService.SetTargetCloseDateAsync</c>.
    /// </summary>
    public DateOnly TargetCloseDate { get; private set; }

    public string? ModifiedBy { get; private set; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    internal FiscalPeriod(int periodNumber, DateOnly startDate, DateOnly endDate)
    {
        Id = Guid.NewGuid();
        PeriodNumber = periodNumber;
        StartDate = startDate;
        EndDate = endDate;
        IsOpen = true;
        TargetCloseDate = endDate.AddDays(5);
    }

    public void SetTargetCloseDate(DateOnly targetCloseDate, string actor)
    {
        TargetCloseDate = targetCloseDate;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private FiscalPeriod()
    {
    }

    public void Close(string actor)
    {
        IsOpen = false;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Reopen(string actor)
    {
        IsOpen = true;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
