using Microsoft.EntityFrameworkCore;
using Modules.Identity.Domain;
using Xunit;

namespace Modules.Identity.IntegrationTests;

public class UserPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_user_with_roles_reads_back_identically()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var user = new User("system/bootstrap", "ahmer.bilal", "Ahmer Bilal", "hashed-value", "ahmer@example.com");
            user.AddRole("MasterData.BusinessPartner.Maintainer");
            user.AddRole("Identity.User.Administrator");
            writeContext.Users.Add(user);
            await writeContext.SaveChangesAsync();
            id = user.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("ahmer.bilal", reloaded!.Username);
        Assert.Equal("ahmer@example.com", reloaded.Email);
        Assert.True(reloaded.IsActive);
        Assert.Equal(2, reloaded.Roles.Count);
        Assert.Contains(reloaded.Roles, r => r.RoleKey == "Identity.User.Administrator");
    }

    [Fact]
    public async Task Username_uniqueness_is_enforced_at_the_database_level()
    {
        await using var context = TestDatabase.CreateContext();
        var first = new User("system/bootstrap", "duplicate.user", "First", "hash1", null);
        context.Users.Add(first);
        await context.SaveChangesAsync();

        var second = new User("system/bootstrap", "duplicate.user", "Second", "hash2", null);
        context.Users.Add(second);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Deactivating_a_user_persists()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var user = new User("system/bootstrap", "to.deactivate", "Someone", "hash", null);
            writeContext.Users.Add(user);
            await writeContext.SaveChangesAsync();
            id = user.Id;

            user.Deactivate("admin");
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Users.FirstAsync(u => u.Id == id);
        Assert.False(reloaded.IsActive);
        Assert.Equal("admin", reloaded.ModifiedBy);
    }

    [Fact]
    public async Task Removing_a_role_persists_and_cascade_delete_removes_all_roles()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var user = new User("system/bootstrap", "role.test", "Someone", "hash", null);
            user.AddRole("RoleA");
            user.AddRole("RoleB");
            writeContext.Users.Add(user);
            await writeContext.SaveChangesAsync();
            id = user.Id;

            user.RemoveRole("RoleA");
            await writeContext.SaveChangesAsync();
        }

        await using (var readContext = TestDatabase.CreateContext())
        {
            var reloaded = await readContext.Users.Include(u => u.Roles).FirstAsync(u => u.Id == id);
            Assert.Single(reloaded.Roles);
            Assert.Equal("RoleB", reloaded.Roles.Single().RoleKey);
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var user = await deleteContext.Users.FirstAsync(u => u.Id == id);
            deleteContext.Users.Remove(user);
            await deleteContext.SaveChangesAsync();
        }

        await using var verifyContext = TestDatabase.CreateContext();
        var remaining = await verifyContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM identity.user_roles WHERE user_id = {id}")
            .SingleAsync();
        Assert.Equal(0, remaining);
    }
}
