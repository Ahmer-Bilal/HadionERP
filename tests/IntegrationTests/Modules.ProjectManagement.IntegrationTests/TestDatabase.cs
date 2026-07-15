using Microsoft.EntityFrameworkCore;
using Modules.ProjectManagement.Infrastructure;

// Same "disable parallelization across test classes sharing one real Postgres database" reasoning as every
// other module's IntegrationTests assembly — see those attributes for the full explanation.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace Modules.ProjectManagement.IntegrationTests;

/// <summary>
/// Points at the real, separate erp_platform_test database's "projectmanagement" schema — same Docker/
/// Testcontainers-unavailable deviation as every other module's IntegrationTests. The connection string
/// comes from an environment variable, never a hardcoded value committed to source control.
/// </summary>
internal static class TestDatabase
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ERP_PROJECTMANAGEMENT_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ERP_PROJECTMANAGEMENT_TEST_CONNECTION before running these integration tests, e.g.: " +
                "Host=localhost;Port=5432;Database=erp_platform_test;Username=postgres;Password=...");

    public static ProjectManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ProjectManagementDbContext(options);
    }

    public static async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE projectmanagement.projects CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE projectmanagement.number_range_counters");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE projectmanagement.workflow_instances");
    }
}
