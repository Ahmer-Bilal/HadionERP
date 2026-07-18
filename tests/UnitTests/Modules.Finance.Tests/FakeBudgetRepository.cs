using Modules.Finance.Application;
using Modules.Finance.Domain;
using Platform.Core;

namespace Modules.Finance.Tests;

internal sealed class FakeBudgetRepository : IBudgetRepository
{
    private readonly Dictionary<Guid, Budget> _budgets = new();

    public Task<Budget?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_budgets.GetValueOrDefault(id));

    public Task<IReadOnlyList<Budget>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Budget>>(
            _budgets.Values.OrderByDescending(b => b.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_budgets.Count);

    public Task<Budget?> GetApprovedAsync(Guid costCenterId, int fiscalYear, CancellationToken cancellationToken = default) =>
        Task.FromResult(_budgets.Values.FirstOrDefault(b =>
            b.CostCenterId == costCenterId && b.FiscalYear == fiscalYear && b.Status == BusinessObjectStatus.Approved));

    public Task<Budget?> GetActiveByCostCenterAndYearAsync(Guid costCenterId, int fiscalYear, CancellationToken cancellationToken = default) =>
        Task.FromResult(_budgets.Values.FirstOrDefault(b =>
            b.CostCenterId == costCenterId && b.FiscalYear == fiscalYear && b.Status != BusinessObjectStatus.Rejected));

    public void Add(Budget budget) => _budgets[budget.Id] = budget;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
