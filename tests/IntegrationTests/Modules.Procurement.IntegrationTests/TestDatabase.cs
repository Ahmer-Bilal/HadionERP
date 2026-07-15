using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Infrastructure;

// Same "disable parallelization across test classes sharing one real Postgres database" reasoning as
// Modules.Finance.IntegrationTests/Modules.MasterData.IntegrationTests — see those assemblies' identical
// attribute for the full explanation.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace Modules.Procurement.IntegrationTests;

/// <summary>
/// Points at the real, separate erp_platform_test database's "procurement" schema — same Docker/
/// Testcontainers-unavailable deviation as Modules.Finance.IntegrationTests. The connection string comes
/// from an environment variable, never a hardcoded value committed to source control.
/// </summary>
internal static class TestDatabase
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ERP_PROCUREMENT_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ERP_PROCUREMENT_TEST_CONNECTION before running these integration tests, e.g.: " +
                "Host=localhost;Port=5432;Database=erp_platform_test;Username=postgres;Password=...");

    public static ProcurementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProcurementDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ProcurementDbContext(options);
    }

    public static async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE procurement.vendor_prequalifications CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE procurement.purchase_requisitions CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE procurement.requests_for_quotation CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE procurement.purchase_orders CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE procurement.goods_receipt_notes CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE procurement.number_range_counters");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE procurement.workflow_instances");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE procurement.attachments CASCADE");
    }
}
