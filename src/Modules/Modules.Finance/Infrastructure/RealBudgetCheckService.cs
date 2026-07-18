using Modules.Finance.Application;
using Modules.Finance.Contracts;

namespace Modules.Finance.Infrastructure;

/// <summary>
/// Implements the published <see cref="IBudgetCheckService"/> contract for real — replaces
/// <c>PassThroughBudgetCheckService</c>, which unconditionally allowed everything because no
/// <see cref="Domain.Budget"/> entity existed yet to check against (see that entity's own doc comment for
/// the full "what this does and doesn't check" reasoning).
///
/// Looks up the one Approved <see cref="Domain.Budget"/> for the given Cost Center and the current calendar
/// year. No budget on file for that combination means budget control simply isn't configured there yet —
/// <c>Allowed = true</c>, the same "opt-in enforcement" every other module in this platform uses for a
/// feature that hasn't been set up (e.g. a Journal Line with no Cost Center at all is legal too). Once a
/// budget IS on file, the amount being checked must not exceed it.
/// </summary>
public sealed class RealBudgetCheckService : IBudgetCheckService
{
    private readonly IBudgetRepository _repository;

    public RealBudgetCheckService(IBudgetRepository repository) => _repository = repository;

    public async Task<BudgetCheckResult> CheckAsync(Guid costCenterId, decimal amount, CancellationToken cancellationToken = default)
    {
        var fiscalYear = DateTimeOffset.UtcNow.Year;
        var budget = await _repository.GetApprovedAsync(costCenterId, fiscalYear, cancellationToken);

        if (budget is null)
            return new BudgetCheckResult(Allowed: true, Reason: null);

        if (amount > budget.Amount)
            return new BudgetCheckResult(
                Allowed: false,
                Reason: $"Amount {amount} exceeds the approved budget of {budget.Amount} for cost center " +
                        $"{costCenterId}, fiscal year {fiscalYear}.");

        return new BudgetCheckResult(Allowed: true, Reason: null);
    }
}
