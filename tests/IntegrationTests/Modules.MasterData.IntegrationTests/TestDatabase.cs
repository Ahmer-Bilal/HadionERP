using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Infrastructure;

namespace Modules.MasterData.IntegrationTests;

/// <summary>
/// Points at the real, separate erp_platform_test database (Docker/Testcontainers isn't available on
/// this development machine — see Modules.MasterData/README.md for the disclosed deviation from
/// docs/architecture/05-engineering-standards.md #1's Testcontainers recommendation). The connection
/// string comes from an environment variable, never a hardcoded value committed to source control.
/// </summary>
internal static class TestDatabase
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ERP_MASTERDATA_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ERP_MASTERDATA_TEST_CONNECTION before running these integration tests, e.g.: " +
                "Host=localhost;Port=5432;Database=erp_platform_test;Username=postgres;Password=...");

    public static MasterDataDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MasterDataDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new MasterDataDbContext(options);
    }

    /// <summary>Wipes all tables so each test starts from a clean, known state — tests share one real
    /// database rather than each getting an isolated container. CASCADE is required now that
    /// business_partner_addresses/contacts hold a foreign key into business_partners.</summary>
    public static async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE masterdata.business_partners CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE masterdata.number_range_counters");
    }
}
