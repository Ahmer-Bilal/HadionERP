using Modules.Finance.Domain;

namespace Modules.Finance.Application;

/// <summary>The persistence port for Budgets — same dependency-inversion shape as
/// <see cref="IJournalEntryRepository"/>.</summary>
public interface IBudgetRepository
{
    Task<Budget?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Budget>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>The one Approved budget for a Cost Center/fiscal year, if any — the exact lookup
    /// <c>RealBudgetCheckService.CheckAsync</c> needs. Null means "no budget control configured for this
    /// combination," not "an amount of zero" — see <see cref="Budget"/>'s own doc comment on why an
    /// unconfigured combination is allowed through rather than blocked.</summary>
    Task<Budget?> GetApprovedAsync(Guid costCenterId, int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>Any not-Rejected budget (Draft, Submitted, or Approved) already recorded for a Cost
    /// Center/fiscal year — used by <c>BudgetService.CreateAsync</c> to reject a second one before it can
    /// ever become ambiguous which Approved budget a check should use. A prior Rejected budget for the same
    /// combination never blocks a new one — rejecting is exactly how you free the combination up again.
    /// </summary>
    Task<Budget?> GetActiveByCostCenterAndYearAsync(Guid costCenterId, int fiscalYear, CancellationToken cancellationToken = default);

    void Add(Budget budget);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
