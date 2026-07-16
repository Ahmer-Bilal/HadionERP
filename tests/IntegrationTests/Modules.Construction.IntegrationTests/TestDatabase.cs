using Microsoft.EntityFrameworkCore;
using Modules.Construction.Infrastructure;

// Same "disable parallelization across test classes sharing one real Postgres database" reasoning as every
// other module's IntegrationTests assembly — see those attributes for the full explanation.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace Modules.Construction.IntegrationTests;

/// <summary>
/// Points at the real, separate erp_platform_test database's "construction" schema — same Docker/
/// Testcontainers-unavailable deviation as every other module's IntegrationTests. The connection string
/// comes from an environment variable, never a hardcoded value committed to source control.
/// </summary>
internal static class TestDatabase
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ERP_CONSTRUCTION_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ERP_CONSTRUCTION_TEST_CONNECTION before running these integration tests, e.g.: " +
                "Host=localhost;Port=5432;Database=erp_platform_test;Username=postgres;Password=...");

    public static ConstructionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConstructionDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ConstructionDbContext(options);
    }

    public static async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE construction.contracts CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE construction.subcontracts CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE construction.number_range_counters");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE construction.workflow_instances");
    }
}
