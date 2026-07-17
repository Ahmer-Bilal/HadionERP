using Modules.Finance.Contracts;

namespace Modules.Finance.Infrastructure;

/// <summary>
/// Implements the published <see cref="IBudgetCheckService"/> contract — proves the cross-module boundary
/// docs/architecture/01-overview.md §3.2 names as its own worked example ("Procurement asks
/// Finance's IBudgetCheckService before releasing a PO"), but there is no real budget data to check against
/// yet: Budget Control is explicitly deferred Finance depth (PROGRESS.md, Phase 1 exit criteria), not built.
/// Always returns <c>Allowed = true</c> until Budget Control exists — disclosed here rather than silently
/// faking enforcement against numbers that don't exist. A future Budget Control slice replaces this class's
/// body (not the interface) with a real check against posted commitments/actuals per cost center per
/// fiscal year.
/// </summary>
public sealed class PassThroughBudgetCheckService : IBudgetCheckService
{
    public Task<BudgetCheckResult> CheckAsync(Guid costCenterId, decimal amount, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BudgetCheckResult(Allowed: true, Reason: null));
}
