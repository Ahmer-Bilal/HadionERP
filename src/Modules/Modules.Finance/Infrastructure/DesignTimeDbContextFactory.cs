using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Finance.Infrastructure;

/// <summary>
/// Lets `dotnet ef migrations add/remove` run against this project directly, without needing the full
/// Gateway.Api host — same pattern as Modules.MasterData's own factory. Reads the connection string from
/// an environment variable, never a hardcoded value. Finance's schema lives in the same physical Postgres
/// database as MasterData's (`erp_platform_dev`/`erp_platform_test`) — this is a schema-ownership
/// boundary, not a separate database — so the same connection string value works for both, just passed
/// via its own env var name to keep each module's tooling independent.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ERP_FINANCE_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the ERP_FINANCE_CONNECTION environment variable before running `dotnet ef` commands.");

        var optionsBuilder = new DbContextOptionsBuilder<FinanceDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FinanceDbContext(optionsBuilder.Options);
    }
}
