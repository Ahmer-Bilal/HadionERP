using Microsoft.EntityFrameworkCore;
using Modules.Identity.Infrastructure;

// Same "disable parallelization across test classes sharing one real Postgres database" reasoning as every
// other module's IntegrationTests assembly — see those attributes for the full explanation.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace Modules.Identity.IntegrationTests;

/// <summary>
/// Points at the real, separate erp_platform_test database's "identity" schema — same Docker/Testcontainers
/// -unavailable deviation as every other module's IntegrationTests. The connection string comes from an
/// environment variable, never a hardcoded value committed to source control.
/// </summary>
internal static class TestDatabase
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ERP_IDENTITY_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ERP_IDENTITY_TEST_CONNECTION before running these integration tests, e.g.: " +
                "Host=localhost;Port=5432;Database=erp_platform_test;Username=postgres;Password=...");

    public static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new IdentityDbContext(options);
    }

    public static async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE identity.users CASCADE");
    }
}
