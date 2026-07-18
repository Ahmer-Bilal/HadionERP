using Platform.Core;

namespace Modules.Finance.Domain;

/// <summary>
/// One Cost Center's approved spending ceiling for one fiscal year — the entity
/// `UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md` calls "the single clearest 'wire exists, nothing on the
/// other end' case in the whole audit": <c>Modules.Finance.Contracts.IBudgetCheckService</c> has been
/// called by Procurement since Phase 2, but until this entity existed there was nothing real to check
/// against (see the deleted <c>PassThroughBudgetCheckService</c>, which this replaces).
///
/// Follows the same simpler Draft → Submit → Approve/Reject lifecycle as <see cref="JournalEntry"/>, not
/// <see cref="CostCenter"/>'s mutable-after-approval shape: a Budget is a control amount, not descriptive
/// master data, so once Approved its <see cref="Amount"/> is fixed — a revision creates a new Budget record
/// for the same Cost Center/fiscal year rather than silently editing the approved figure. A formal Budget
/// Supplement/Return mechanism (SAP's own approach to revising a budget after approval) is real future
/// depth, not built here.
///
/// Deliberately scoped to "does a single amount exceed this cost center's approved annual total" —
/// <see cref="Amount"/> is checked as a whole, not against a running total of everything already committed
/// this year. Real availability control (cumulative committed + actual vs. available, SAP's own
/// Assigned/Committed/Actual tracking) would need either Finance reading Procurement's own committed PO
/// amounts (a new cross-module read contract reversing the established one-directional
/// Procurement-depends-on-Finance.Contracts boundary, docs/architecture/01-overview.md §3.2) or this
/// service's check becoming stateful (consuming budget as a side effect of a passing check) — both real
/// design decisions deliberately left for a later slice rather than picked silently here.
/// </summary>
public sealed class Budget : BusinessObject
{
    /// <summary>The Cost Center this budget controls — validated against
    /// <c>Modules.MasterData.Contracts.ICostCenterLookup</c> at creation, the same cross-module reference
    /// pattern <see cref="JournalLine.GLAccountId"/>/<see cref="JournalLine.CostCenterId"/> already use.
    /// </summary>
    public Guid CostCenterId { get; private set; }

    /// <summary>The fiscal year this budget covers — a plain calendar-year integer, the same
    /// "no separate Fiscal Period entity yet" simplification <c>INumberRangeService</c> already uses
    /// (<c>DateTimeOffset.UtcNow.Year</c>). A real Fiscal Year/Period Business Object with open/locked
    /// state remains a separate, not-yet-built ROADMAP.md item.</summary>
    public int FiscalYear { get; private set; }

    /// <summary>The approved annual ceiling for this Cost Center/fiscal year. Fixed once set — see this
    /// class's own doc comment for why there is no Update method.</summary>
    public decimal Amount { get; private set; }

    public Budget(string createdBy, Guid costCenterId, int fiscalYear, decimal amount)
        : base(createdBy)
    {
        if (fiscalYear < 2000 || fiscalYear > 2100)
            throw new ArgumentOutOfRangeException(nameof(fiscalYear), fiscalYear, "Fiscal year is out of range.");
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Budget amount must be positive.");

        CostCenterId = costCenterId;
        FiscalYear = fiscalYear;
        Amount = amount;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private Budget()
    {
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
