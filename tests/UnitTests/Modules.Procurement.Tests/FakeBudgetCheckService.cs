using Modules.Finance.Contracts;

namespace Modules.Procurement.Tests;

/// <summary>In-memory stand-in for Modules.Finance.Contracts.IBudgetCheckService — proves
/// PurchaseOrderService's own call-the-budget-check-before-submit logic without a real Finance database.
/// Defaults to allowing everything (matching the real <c>PassThroughBudgetCheckService</c>); a test can
/// deny a specific cost center to prove the submit-blocking path.</summary>
internal sealed class FakeBudgetCheckService : IBudgetCheckService
{
    private readonly HashSet<Guid> _deniedCostCenterIds = new();

    public void Deny(Guid costCenterId) => _deniedCostCenterIds.Add(costCenterId);

    public Task<BudgetCheckResult> CheckAsync(Guid costCenterId, decimal amount, CancellationToken cancellationToken = default) =>
        Task.FromResult(_deniedCostCenterIds.Contains(costCenterId)
            ? new BudgetCheckResult(false, "Insufficient budget.")
            : new BudgetCheckResult(true, null));
}
