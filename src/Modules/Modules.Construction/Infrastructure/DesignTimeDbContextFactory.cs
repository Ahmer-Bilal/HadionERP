using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Construction.Infrastructure;

/// <summary>
/// Lets `dotnet ef migrations add/remove` run against this project directly, without needing the full
/// Gateway.Api host — same pattern as every other module's own factory. Reads the connection string from an
/// environment variable, never a hardcoded value. This module's schema lives in the same physical Postgres
/// database as every other module's (`erp_platform_dev`/`erp_platform_test`) — a schema-ownership boundary,
/// not a separate database — so the same connection string value works for all of them, just passed via its
/// own env var name to keep each module's tooling independent.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConstructionDbContext>
{
    public ConstructionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ERP_CONSTRUCTION_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the ERP_CONSTRUCTION_CONNECTION environment variable before running `dotnet ef` commands.");

        var optionsBuilder = new DbContextOptionsBuilder<ConstructionDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ConstructionDbContext(optionsBuilder.Options);
    }
}
