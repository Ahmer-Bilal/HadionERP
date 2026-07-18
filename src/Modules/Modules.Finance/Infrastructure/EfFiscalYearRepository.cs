using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Infrastructure;

public sealed class EfFiscalYearRepository : IFiscalYearRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfFiscalYearRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    // Tracked (not AsNoTracking): ClosePeriodAsync/ReopenPeriodAsync load via GetAsync then mutate + Save,
    // same rationale as EfJournalEntryRepository.GetAsync.
    public Task<FiscalYear?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.FiscalYears.Include(y => y.Periods).FirstOrDefaultAsync(y => y.Id == id, cancellationToken);

    public Task<FiscalYear?> GetByYearAsync(int year, CancellationToken cancellationToken = default) =>
        _dbContext.FiscalYears.Include(y => y.Periods).FirstOrDefaultAsync(y => y.Year == year, cancellationToken);

    public async Task<IReadOnlyList<FiscalYear>> ListAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.FiscalYears.AsNoTracking().Include(y => y.Periods)
            .OrderByDescending(y => y.Year).ToListAsync(cancellationToken);

    public Task<FiscalPeriod?> GetPeriodByDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        _dbContext.FiscalPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => date >= p.StartDate && date <= p.EndDate, cancellationToken);

    public void Add(FiscalYear fiscalYear) => _dbContext.FiscalYears.Add(fiscalYear);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
