using Modules.Finance.Application;
using Modules.Finance.Domain;
using Modules.Identity.Contracts;
using Platform.Audit;
using Platform.Security;

namespace Modules.Finance.Tests;

public class ClosingActivityServiceTests
{
    private static readonly Guid ManagerId = Guid.NewGuid();
    private static readonly Guid AccountantId = Guid.NewGuid();
    private static readonly Guid OtherPersonId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["finance.manager"] = new[] { FiscalYearSecurity.AdministratorRoleKey },
            ["sana.ali"] = Array.Empty<string>(),
            ["imran.khan"] = Array.Empty<string>(),
        });

    private static FakeUserLookup BuildUserLookup()
    {
        var lookup = new FakeUserLookup();
        lookup.Add(new UserSummary(ManagerId, "finance.manager", "Finance Manager", true, new[] { "Finance.FiscalYear.Administrator" }));
        lookup.Add(new UserSummary(AccountantId, "sana.ali", "Sana Ali", true, new[] { "Accountant" }));
        lookup.Add(new UserSummary(OtherPersonId, "imran.khan", "Imran Khan", true, new[] { "Senior Accountant" }));
        return lookup;
    }

    private sealed class Fixture
    {
        public required ClosingActivityService Service;
        public required FakeFiscalYearRepository FiscalYears;
        public required FakeAPInvoiceRepository ApInvoices;
        public required FakeARInvoiceRepository ArInvoices;
        public required FakeJournalEntryRepository JournalEntries;
        public required FakeBankAccountRepository BankAccounts;
    }

    private static Fixture BuildFixture()
    {
        var fiscalYears = new FakeFiscalYearRepository();
        var apInvoices = new FakeAPInvoiceRepository();
        var arInvoices = new FakeARInvoiceRepository();
        var journalEntries = new FakeJournalEntryRepository();
        var bankAccounts = new FakeBankAccountRepository();
        var auditLog = new InMemoryAuditLog();
        var securityCatalog = new InMemorySecurityCatalog(
            new[] { FiscalYearSecurity.AdministratorRole },
            new[] { FiscalYearSecurity.AdministratorDuty });

        var service = new ClosingActivityService(
            new FakeClosingActivityRepository(), fiscalYears, apInvoices, arInvoices, journalEntries, bankAccounts,
            BuildUserLookup(), new AuditRecorder(auditLog), auditLog,
            new AuthorizationService(securityCatalog), BuildActorRoles());

        return new Fixture
        {
            Service = service, FiscalYears = fiscalYears, ApInvoices = apInvoices,
            ArInvoices = arInvoices, JournalEntries = journalEntries, BankAccounts = bankAccounts,
        };
    }

    [Fact]
    public async Task ListForPeriod_generates_all_ten_fixed_activities_on_first_call()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);

        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");

        Assert.Equal(10, activities.Count);
        Assert.Equal(ClosingActivityCatalog.All.Select(d => d.Key), activities.OrderBy(a => a.SequenceNumber).Select(a => a.ActivityKey));
    }

    [Fact]
    public async Task AccountsPayable_gets_one_step_per_pending_invoice_in_the_period()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        var invoice = new APInvoice("finance.manager", Guid.NewGuid(), "VINV-001", new DateOnly(2026, 5, 10), "Test", Guid.NewGuid(), Guid.NewGuid(), 1000m);
        fx.ApInvoices.Add(invoice);

        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        var ap = activities.Single(a => a.ActivityKey == ClosingActivityCatalog.AccountsPayable);

        Assert.Equal(1, ap.TotalSteps);
        Assert.Equal(0, ap.CompletedSteps);
        Assert.Equal("NotStarted", ap.Status);
    }

    [Fact]
    public async Task A_posted_invoice_auto_completes_its_step_on_refresh()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        var invoice = new APInvoice("finance.manager", Guid.NewGuid(), "VINV-001", new DateOnly(2026, 5, 10), "Test", Guid.NewGuid(), Guid.NewGuid(), 1000m);
        fx.ApInvoices.Add(invoice);

        await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager"); // generates the step, Draft = pending

        invoice.Submit("finance.manager");
        invoice.Approve("finance.manager");
        invoice.LinkJournalEntry(Guid.NewGuid());
        invoice.Post("finance.manager"); // now resolved

        var refreshed = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        var ap = refreshed.Single(a => a.ActivityKey == ClosingActivityCatalog.AccountsPayable);

        Assert.Equal(1, ap.CompletedSteps);
        Assert.Equal("Completed", ap.Status);
    }

    [Fact]
    public async Task An_activity_with_no_pending_documents_reads_as_Completed_with_zero_steps()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);

        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        var ap = activities.Single(a => a.ActivityKey == ClosingActivityCatalog.AccountsPayable);

        Assert.Equal(0, ap.TotalSteps);
        Assert.Equal("Completed", ap.Status);
    }

    [Fact]
    public async Task Assign_requires_the_Administer_privilege()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        var activity = activities.First();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            fx.Service.AssignAsync(activity.Id, new AssignClosingActivityRequest(AccountantId, null), "sana.ali"));
    }

    [Fact]
    public async Task Assign_succeeds_for_an_administrator_and_is_visible_on_reread()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        var activity = activities.First();

        var assigned = await fx.Service.AssignAsync(activity.Id, new AssignClosingActivityRequest(AccountantId, null), "finance.manager");

        Assert.Equal(AccountantId, assigned.AssignedToUserId);
        Assert.Equal("Sana Ali", assigned.AssignedToDisplayName);
    }

    [Fact]
    public async Task ToggleStep_is_rejected_for_someone_who_is_not_the_assignee_or_an_administrator()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        // Inventory Closing is always manual/single-step, safe to toggle in this test
        var activity = activities.Single(a => a.ActivityKey == ClosingActivityCatalog.InventoryClosing);
        await fx.Service.AssignAsync(activity.Id, new AssignClosingActivityRequest(AccountantId, null), "finance.manager");
        var step = activity.Steps.Single();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            fx.Service.ToggleStepAsync(activity.Id, step.Id, true, "imran.khan"));
    }

    [Fact]
    public async Task ToggleStep_succeeds_for_the_assigned_person_and_moves_the_activity_to_Completed()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        var activity = activities.Single(a => a.ActivityKey == ClosingActivityCatalog.InventoryClosing);
        await fx.Service.AssignAsync(activity.Id, new AssignClosingActivityRequest(AccountantId, null), "finance.manager");
        var step = activity.Steps.Single();

        var result = await fx.Service.ToggleStepAsync(activity.Id, step.Id, true, "sana.ali");

        Assert.Equal("Completed", result.Status);
        Assert.Equal(1, result.CompletedSteps);
    }

    [Fact]
    public async Task ToggleStep_rejects_an_auto_tracked_step()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        var invoice = new APInvoice("finance.manager", Guid.NewGuid(), "VINV-001", new DateOnly(2026, 5, 10), "Test", Guid.NewGuid(), Guid.NewGuid(), 1000m);
        fx.ApInvoices.Add(invoice);
        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        var activity = activities.Single(a => a.ActivityKey == ClosingActivityCatalog.AccountsPayable);
        await fx.Service.AssignAsync(activity.Id, new AssignClosingActivityRequest(AccountantId, null), "finance.manager");
        var step = activity.Steps.Single();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Service.ToggleStepAsync(activity.Id, step.Id, true, "sana.ali"));
    }

    [Fact]
    public async Task SetBlocked_by_the_assignee_is_reflected_in_Insights()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        var activities = await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");
        var activity = activities.Single(a => a.ActivityKey == ClosingActivityCatalog.InventoryClosing);
        await fx.Service.AssignAsync(activity.Id, new AssignClosingActivityRequest(AccountantId, null), "finance.manager");

        await fx.Service.SetBlockedAsync(activity.Id, true, "sana.ali");
        var insights = await fx.Service.GetInsightsAsync(year.Id, 5, "finance.manager");

        Assert.Contains(insights, i => i.Severity == "AttentionRequired" && i.Message.Contains("Inventory Closing"));
    }

    [Fact]
    public async Task GetInsights_reports_OnTrack_when_nothing_is_blocked_or_overdue()
    {
        var fx = BuildFixture();
        var year = new FiscalYear("finance.manager", 2026);
        fx.FiscalYears.Add(year);
        await fx.Service.ListForPeriodAsync(year.Id, 5, "finance.manager");

        var insights = await fx.Service.GetInsightsAsync(year.Id, 5, "finance.manager");

        Assert.Contains(insights, i => i.Severity == "OnTrack");
    }
}
