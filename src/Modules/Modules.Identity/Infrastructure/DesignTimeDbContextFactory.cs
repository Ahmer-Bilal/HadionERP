using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Identity.Infrastructure;

/// <summary>
/// Lets `dotnet ef migrations add/remove` run against this project directly, without needing the full
/// Gateway.Api host. Reads the connection string from an environment variable, never a hardcoded value —
/// this file is committed to source control and must never contain a real password.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ERP_IDENTITY_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the ERP_IDENTITY_CONNECTION environment variable before running `dotnet ef` commands.");

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
