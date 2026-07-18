using Microsoft.EntityFrameworkCore;
using Modules.Finance.Domain;
using Xunit;

namespace Modules.Finance.IntegrationTests;

public class ClosingActivityPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_activity_with_its_steps_reads_back_identically()
    {
        Guid periodId;
        Guid activityId;
        var linkedInvoiceId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var year = new FiscalYear("ahmer.bilal", 2026);
            writeContext.FiscalYears.Add(year);
            await writeContext.SaveChangesAsync();
            periodId = year.Periods.Single(p => p.PeriodNumber == 5).Id;

            var activity = new ClosingActivity(periodId, ClosingActivityCatalog.AccountsPayable, 2);
            activity.AddStep("Close AP Invoice 'FIN-AP-2026-000001'", "APInvoice", linkedInvoiceId);
            activity.AddStep("Reconcile account", null, null);
            activity.RefreshStatus("system/auto-tracked");
            writeContext.ClosingActivities.Add(activity);
            await writeContext.SaveChangesAsync();
            activityId = activity.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.ClosingActivities.Include(a => a.Steps).FirstOrDefaultAsync(a => a.Id == activityId);

        Assert.NotNull(reloaded);
        Assert.Equal(periodId, reloaded!.FiscalPeriodId);
        Assert.Equal(ClosingActivityCatalog.AccountsPayable, reloaded.ActivityKey);
        Assert.Equal(2, reloaded.Steps.Count);
        var linkedStep = reloaded.Steps.Single(s => s.LinkedDocumentId == linkedInvoiceId);
        Assert.Equal("APInvoice", linkedStep.LinkedDocumentType);
        Assert.True(linkedStep.IsAutoTracked);
        var manualStep = reloaded.Steps.Single(s => s.LinkedDocumentId == null);
        Assert.False(manualStep.IsAutoTracked);
    }

    [Fact]
    public async Task Assigning_and_completing_a_step_persists_through_a_fresh_DbContext()
    {
        Guid activityId;
        var userId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var year = new FiscalYear("ahmer.bilal", 2026);
            writeContext.FiscalYears.Add(year);
            await writeContext.SaveChangesAsync();
            var periodId = year.Periods.Single(p => p.PeriodNumber == 5).Id;

            var activity = new ClosingActivity(periodId, ClosingActivityCatalog.InventoryClosing, 4);
            var newStep = activity.AddStep("Confirm Inventory Closing complete for this period", null, null);
            activity.Assign(userId, new DateOnly(2026, 6, 2), "finance.manager");
            writeContext.ClosingActivities.Add(activity);
            await writeContext.SaveChangesAsync();
            activityId = activity.Id;

            newStep.SetCompleted(true, "mustafa.saleem");
            activity.RefreshStatus("mustafa.saleem");
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.ClosingActivities.Include(a => a.Steps).FirstAsync(a => a.Id == activityId);

        Assert.Equal(userId, reloaded.AssignedToUserId);
        Assert.Equal(new DateOnly(2026, 6, 2), reloaded.DueDate);
        Assert.Equal(ClosingActivityStatus.Completed, reloaded.Status);
        var step = reloaded.Steps.Single();
        Assert.True(step.IsCompleted);
        Assert.Equal("mustafa.saleem", step.CompletedBy);
    }

    [Fact]
    public async Task Deleting_a_fiscal_year_cascades_to_periods_and_a_period_delete_cascades_to_activities()
    {
        Guid yearId;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var year = new FiscalYear("ahmer.bilal", 2026);
            writeContext.FiscalYears.Add(year);
            await writeContext.SaveChangesAsync();
            yearId = year.Id;
            var periodId = year.Periods.Single(p => p.PeriodNumber == 5).Id;

            var activity = new ClosingActivity(periodId, ClosingActivityCatalog.ManagementReview, 10);
            activity.AddStep("Final review and sign-off", null, null);
            writeContext.ClosingActivities.Add(activity);
            await writeContext.SaveChangesAsync();
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var year = await deleteContext.FiscalYears.FirstAsync(y => y.Id == yearId);
            deleteContext.FiscalYears.Remove(year);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingPeriods = await readContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM finance.fiscal_periods")
            .SingleAsync();
        Assert.Equal(0, remainingPeriods);
        var remainingActivities = await readContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM finance.closing_activities")
            .SingleAsync();
        Assert.Equal(0, remainingActivities);
        var remainingSteps = await readContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM finance.closing_activity_steps")
            .SingleAsync();
        Assert.Equal(0, remainingSteps);
    }
}
