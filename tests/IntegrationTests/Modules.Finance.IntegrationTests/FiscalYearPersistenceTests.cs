using Microsoft.EntityFrameworkCore;
using Modules.Finance.Domain;
using Xunit;

namespace Modules.Finance.IntegrationTests;

public class FiscalYearPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_fiscal_year_with_its_12_periods_reads_back_identically()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var year = new FiscalYear("ahmer.bilal", 2026);
            writeContext.FiscalYears.Add(year);
            await writeContext.SaveChangesAsync();
            id = year.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.FiscalYears.Include(y => y.Periods).FirstOrDefaultAsync(y => y.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal(2026, reloaded!.Year);
        Assert.Equal(12, reloaded.Periods.Count);
        var may = reloaded.Periods.Single(p => p.PeriodNumber == 5);
        Assert.Equal(new DateOnly(2026, 5, 1), may.StartDate);
        Assert.Equal(new DateOnly(2026, 5, 31), may.EndDate);
        Assert.Equal(may.EndDate.AddDays(5), may.TargetCloseDate);
    }

    [Fact]
    public async Task Closing_a_period_persists_IsOpen_and_ModifiedBy_through_a_fresh_DbContext()
    {
        Guid yearId;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var year = new FiscalYear("ahmer.bilal", 2026);
            writeContext.FiscalYears.Add(year);
            await writeContext.SaveChangesAsync();
            yearId = year.Id;
        }

        await using (var closeContext = TestDatabase.CreateContext())
        {
            var year = await closeContext.FiscalYears.Include(y => y.Periods).FirstAsync(y => y.Id == yearId);
            year.Periods.Single(p => p.PeriodNumber == 5).Close("finance.manager");
            await closeContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.FiscalYears.Include(y => y.Periods).FirstAsync(y => y.Id == yearId);
        var may = reloaded.Periods.Single(p => p.PeriodNumber == 5);

        Assert.False(may.IsOpen);
        Assert.Equal("finance.manager", may.ModifiedBy);
        Assert.NotNull(may.ModifiedAt);
    }

    [Fact]
    public async Task GetPeriodByDateAsync_finds_the_covering_period_via_the_real_repository()
    {
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var year = new FiscalYear("ahmer.bilal", 2026);
            writeContext.FiscalYears.Add(year);
            await writeContext.SaveChangesAsync();
        }

        var repository = new Modules.Finance.Infrastructure.EfFiscalYearRepository(TestDatabase.CreateContext());
        var period = await repository.GetPeriodByDateAsync(new DateOnly(2026, 5, 15));

        Assert.NotNull(period);
        Assert.Equal(5, period!.PeriodNumber);
    }
}
