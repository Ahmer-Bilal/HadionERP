using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.MasterData.Infrastructure;

/// <summary>
/// Lets `dotnet ef migrations add/remove` run against this project directly, without needing the full
/// Gateway.Api host. Reads the connection string from an environment variable, never a hardcoded value —
/// this file is committed to source control and must never contain a real password (the running
/// application itself gets its connection string from .NET User Secrets, configured on Gateway.Api, not
/// from this factory — this is design-time tooling only).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MasterDataDbContext>
{
    public MasterDataDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ERP_MASTERDATA_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the ERP_MASTERDATA_CONNECTION environment variable before running `dotnet ef` commands.");

        var optionsBuilder = new DbContextOptionsBuilder<MasterDataDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MasterDataDbContext(optionsBuilder.Options);
    }
}
