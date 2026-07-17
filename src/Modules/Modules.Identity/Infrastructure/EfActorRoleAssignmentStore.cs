using Microsoft.EntityFrameworkCore;
using Platform.Security;

namespace Modules.Identity.Infrastructure;

/// <summary>
/// The real, persisted <see cref="IActorRoleAssignmentStore"/> — replaces
/// <see cref="InMemoryActorRoleAssignmentStore"/>'s hardcoded dictionary now that real users/roles exist
/// (`MISSING-FEATURES-AUDIT.md` Part 1 §1). A deactivated or unknown username resolves to zero roles — "denied
/// by default, not granted by default," the same convention the in-memory reference implementation it
/// replaces already established.
///
/// <see cref="IActorRoleAssignmentStore.ResolveRoleKeys"/> is a synchronous method (it's called inline from
/// inside every Application service's <c>BuildPrincipal(actor)</c> helper, across every module, none of
/// which this change touches) — this does a synchronous EF Core query rather than changing that interface's
/// signature solution-wide. A small, disclosed inefficiency (one blocking DB round trip per authorization
/// check), not a correctness issue; revisit only if this interface ever grows an async twin.
/// </summary>
public sealed class EfActorRoleAssignmentStore : IActorRoleAssignmentStore
{
    private readonly IdentityDbContext _dbContext;

    public EfActorRoleAssignmentStore(IdentityDbContext dbContext) => _dbContext = dbContext;

    public IReadOnlyCollection<string> ResolveRoleKeys(string actorId) =>
        _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Username == actorId && u.IsActive)
            .SelectMany(u => u.Roles.Select(r => r.RoleKey))
            .ToArray();
}
