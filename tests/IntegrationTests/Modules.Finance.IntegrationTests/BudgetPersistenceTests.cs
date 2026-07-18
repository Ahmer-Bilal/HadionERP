using Microsoft.EntityFrameworkCore;
using Modules.Finance.Domain;
using Modules.Finance.Infrastructure;
using Platform.Core;
using Xunit;

namespace Modules.Finance.IntegrationTests;

public class BudgetPersistenceTests : IAsyncLifetime
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_budget_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        var costCenterId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var budget = new Budget("ahmer.bilal", costCenterId, CurrentYear, 50000m);
            budget.AssignNumber("FIN-BUD-2026-000001");
            writeContext.Budgets.Add(budget);
            await writeContext.SaveChangesAsync();
            id = budget.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Budgets.FirstOrDefaultAsync(b => b.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("FIN-BUD-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(costCenterId, reloaded.CostCenterId);
        Assert.Equal(CurrentYear, reloaded.FiscalYear);
        Assert.Equal(50000m, reloaded.Amount);
        Assert.Equal(BusinessObjectStatus.Draft, reloaded.Status);
    }

    [Fact]
    public async Task Submit_approve_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var budget = new Budget("ahmer.bilal", Guid.NewGuid(), CurrentYear, 50000m);
            budget.AssignNumber("FIN-BUD-2026-000002");
            writeContext.Budgets.Add(budget);
            await writeContext.SaveChangesAsync();
            budget.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            budget.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            id = budget.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Budgets.FirstAsync(b => b.Id == id);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal(2, reloaded.RowVersion); // Submit, Approve = two transitions
    }

    [Fact]
    public async Task RealBudgetCheckService_denies_an_amount_over_a_real_persisted_approved_budget()
    {
        var costCenterId = Guid.NewGuid();
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var budget = new Budget("ahmer.bilal", costCenterId, CurrentYear, 20000m);
            budget.AssignNumber("FIN-BUD-2026-000003");
            budget.Submit("ahmer.bilal");
            budget.Approve("finance.manager");
            writeContext.Budgets.Add(budget);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var service = new RealBudgetCheckService(new EfBudgetRepository(readContext));

        var withinBudget = await service.CheckAsync(costCenterId, 15000m);
        Assert.True(withinBudget.Allowed);

        var overBudget = await service.CheckAsync(costCenterId, 25000m);
        Assert.False(overBudget.Allowed);
        Assert.NotNull(overBudget.Reason);
    }

    [Fact]
    public async Task RealBudgetCheckService_allows_everything_when_no_budget_is_on_file()
    {
        await using var readContext = TestDatabase.CreateContext();
        var service = new RealBudgetCheckService(new EfBudgetRepository(readContext));

        var result = await service.CheckAsync(Guid.NewGuid(), 1_000_000m);
        Assert.True(result.Allowed);
    }
}
