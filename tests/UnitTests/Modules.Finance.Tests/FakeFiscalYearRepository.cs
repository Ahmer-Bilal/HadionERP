using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

internal sealed class FakeFiscalYearRepository : IFiscalYearRepository
{
    private readonly Dictionary<Guid, FiscalYear> _years = new();

    public Task<FiscalYear?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_years.GetValueOrDefault(id));

    public Task<FiscalYear?> GetByYearAsync(int year, CancellationToken cancellationToken = default) =>
        Task.FromResult(_years.Values.FirstOrDefault(y => y.Year == year));

    public Task<IReadOnlyList<FiscalYear>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FiscalYear>>(_years.Values.OrderByDescending(y => y.Year).ToList());

    public Task<FiscalPeriod?> GetPeriodByDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult(_years.Values.SelectMany(y => y.Periods).FirstOrDefault(p => date >= p.StartDate && date <= p.EndDate));

    public void Add(FiscalYear fiscalYear) => _years[fiscalYear.Id] = fiscalYear;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
