using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Modules.Identity.Domain;

namespace Modules.Identity.Infrastructure;

/// <summary>
/// Idempotent startup seeding for one bootstrap administrator — mirrors
/// `Modules.MasterData.Infrastructure.LookupSeeder`'s "only seed if genuinely missing" pattern. Without
/// this, adding real authentication would make the system unable to bootstrap itself (nobody could log in
/// to create the first user). Only ever creates the bootstrap admin when the `users` table is completely
/// empty — an operator who later renames/deactivates/deletes it is never overridden by this seeder.
/// </summary>
public static class IdentitySeeder
{
    public const string BootstrapUsername = "admin";

    public static async Task SeedAsync(
        IdentityDbContext dbContext, string bootstrapPassword, IReadOnlyCollection<string> allRoleKeys,
        CancellationToken cancellationToken = default)
    {
        var anyUsers = await dbContext.Users.AnyAsync(cancellationToken);
        if (anyUsers) return;

        var admin = new User("system/bootstrap", BootstrapUsername, "System Administrator", "pending", email: null);
        var hasher = new PasswordHasher<User>();
        admin.SetPasswordHash("system/bootstrap", hasher.HashPassword(admin, bootstrapPassword));

        foreach (var roleKey in allRoleKeys)
            admin.AddRole(roleKey);

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
