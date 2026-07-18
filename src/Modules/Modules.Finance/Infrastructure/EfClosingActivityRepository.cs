using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Infrastructure;

public sealed class EfClosingActivityRepository : IClosingActivityRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfClosingActivityRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    public Task<ClosingActivity?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.ClosingActivities.Include(a => a.Steps).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ClosingActivity>> ListForPeriodAsync(Guid fiscalPeriodId, CancellationToken cancellationToken = default) =>
        await _dbContext.ClosingActivities.Include(a => a.Steps)
            .Where(a => a.FiscalPeriodId == fiscalPeriodId)
            .OrderBy(a => a.SequenceNumber)
            .ToListAsync(cancellationToken);

    public void Add(ClosingActivity activity) => _dbContext.ClosingActivities.Add(activity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
