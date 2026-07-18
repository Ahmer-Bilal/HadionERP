namespace Modules.Finance.Domain;

/// <summary>
/// One calendar year's worth of posting periods — the entity `UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md`'s
/// Period Closing Center entry calls for ("nothing today models a fiscal period at all —
/// <c>JournalEntry.PostingDate</c> is a free date with no period concept enforcing it") and the connecting
/// piece behind both that mockup and <see cref="Budget"/>'s own shallow "no real periods" limitation (see
/// that entity's own doc comment).
///
/// Deliberately calendar-year only (January 1 through December 31 of <see cref="Year"/>, exactly 12 monthly
/// <see cref="Periods"/>) — the same simplification <c>Platform.Core.NumberRanges.INumberRangeService</c>
/// and <see cref="Budget.FiscalYear"/> already make. A custom fiscal-year-start-month, a 13th/16th special
/// period for year-end adjustments (both real SAP capabilities), and a formal per-activity closing checklist
/// (the mockup's other request, "a closing checklist with per-activity owner/status") are none of them built
/// here — this slice is the period open/locked state and the posting block alone, deliberately scoped that
/// way rather than silently expanded.
///
/// Not a <see cref="Platform.Core.BusinessObject"/>: opening a fiscal year and closing/reopening one of its
/// periods are immediate-effect, single-privilege-gated actions, the same reasoning
/// <c>Modules.MasterData.Domain.LookupType</c>/<c>LookupValue</c> already established for admin-configured
/// control data in this codebase — not a business transaction that needs a Draft → Submit → Approve
/// workflow.
/// </summary>
public sealed class FiscalYear
{
    private readonly List<FiscalPeriod> _periods = new();

    public Guid Id { get; private set; }

    /// <summary>The calendar year, e.g. 2026 — unique, enforced by <c>FiscalYearService</c> + a DB unique
    /// index, the same "one real row per key" reasoning <see cref="Budget"/> uses for its own Cost
    /// Center/fiscal-year uniqueness.</summary>
    public int Year { get; private set; }

    /// <summary>The 12 calendar-month periods spanning this year, auto-generated at creation — a fiscal
    /// year is never created "empty" the way a Journal Entry starts with zero lines, since periods aren't
    /// user-entered content, they're mechanically derived from the year alone.</summary>
    public IReadOnlyCollection<FiscalPeriod> Periods => _periods.AsReadOnly();

    public string CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public FiscalYear(string createdBy, int year)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), year, "Fiscal year is out of range.");

        Id = Guid.NewGuid();
        Year = year;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;

        for (var month = 1; month <= 12; month++)
        {
            var start = new DateOnly(year, month, 1);
            var end = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            _periods.Add(new FiscalPeriod(month, start, end));
        }
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private FiscalYear()
    {
        CreatedBy = null!;
    }

    /// <summary>The one period whose date range contains <paramref name="date"/>, if this fiscal year
    /// covers that date at all. Null means "this year doesn't reach that far," never an error — a fiscal
    /// year is only ever a partial calendar, and <c>JournalEntryService</c>'s own caller treats "no period
    /// configured for this date" as opt-in-not-enforced, the same reasoning
    /// <c>RealBudgetCheckService</c> treats "no budget on file" as allowed.</summary>
    public FiscalPeriod? FindPeriodFor(DateOnly date) =>
        _periods.FirstOrDefault(p => date >= p.StartDate && date <= p.EndDate);
}
