using Microsoft.EntityFrameworkCore;
using Modules.Finance.Infrastructure;

// Same "disable parallelization across test classes sharing one real Postgres database" reasoning as
// Modules.MasterData.IntegrationTests — see that assembly's identical attribute for the full explanation.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace Modules.Finance.IntegrationTests;

/// <summary>
/// Points at the real, separate erp_platform_test database's "finance" schema — same
/// Docker/Testcontainers-unavailable deviation as Modules.MasterData.IntegrationTests. The connection
/// string comes from an environment variable, never a hardcoded value committed to source control.
/// </summary>
internal static class TestDatabase
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ERP_FINANCE_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ERP_FINANCE_TEST_CONNECTION before running these integration tests, e.g.: " +
                "Host=localhost;Port=5432;Database=erp_platform_test;Username=postgres;Password=...");

    public static FinanceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new FinanceDbContext(options);
    }

    public static async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE finance.payments CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE finance.customer_receipts CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE finance.bank_accounts");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE finance.journal_entries CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE finance.ap_invoices");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE finance.ar_invoices");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE finance.number_range_counters");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE finance.workflow_instances");
    }
}
