using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Procurement.Infrastructure;

/// <summary>
/// Lets `dotnet ef migrations add/remove` run against this project directly, without needing the full
/// Gateway.Api host — same pattern as Modules.MasterData's/Modules.Finance's own factories. Reads the
/// connection string from an environment variable, never a hardcoded value. Procurement's schema lives in
/// the same physical Postgres database as MasterData's/Finance's (`erp_platform_dev`/`erp_platform_test`)
/// — a schema-ownership boundary, not a separate database — so the same connection string value works for
/// all three, just passed via its own env var name to keep each module's tooling independent.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProcurementDbContext>
{
    public ProcurementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ERP_PROCUREMENT_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the ERP_PROCUREMENT_CONNECTION environment variable before running `dotnet ef` commands.");

        var optionsBuilder = new DbContextOptionsBuilder<ProcurementDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ProcurementDbContext(optionsBuilder.Options);
    }
}
