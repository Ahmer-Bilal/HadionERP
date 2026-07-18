using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;
using Platform.Core;

namespace Modules.Finance.Infrastructure;

public sealed class EfBudgetRepository : IBudgetRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfBudgetRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    public Task<Budget?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Budgets.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Budget>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.Budgets.AsNoTracking()
            .OrderByDescending(b => b.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Budgets.CountAsync(cancellationToken);

    // AsNoTracking: RealBudgetCheckService only ever reads this, never mutates it through this query.
    public Task<Budget?> GetApprovedAsync(Guid costCenterId, int fiscalYear, CancellationToken cancellationToken = default) =>
        _dbContext.Budgets.AsNoTracking().FirstOrDefaultAsync(
            b => b.CostCenterId == costCenterId && b.FiscalYear == fiscalYear && b.Status == BusinessObjectStatus.Approved,
            cancellationToken);

    public Task<Budget?> GetActiveByCostCenterAndYearAsync(Guid costCenterId, int fiscalYear, CancellationToken cancellationToken = default) =>
        _dbContext.Budgets.AsNoTracking().FirstOrDefaultAsync(
            b => b.CostCenterId == costCenterId && b.FiscalYear == fiscalYear && b.Status != BusinessObjectStatus.Rejected,
            cancellationToken);

    public void Add(Budget budget) => _dbContext.Budgets.Add(budget);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
