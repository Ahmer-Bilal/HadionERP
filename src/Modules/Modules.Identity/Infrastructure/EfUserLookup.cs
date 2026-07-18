using Microsoft.EntityFrameworkCore;
using Modules.Identity.Contracts;
using Modules.Identity.Domain;

namespace Modules.Identity.Infrastructure;

public sealed class EfUserLookup : IUserLookup
{
    private readonly IdentityDbContext _dbContext;

    public EfUserLookup(IdentityDbContext dbContext) => _dbContext = dbContext;

    public async Task<UserSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.AsNoTracking().Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user is null ? null : ToSummary(user);
    }

    public async Task<IReadOnlyList<UserSummary>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users.AsNoTracking().Include(u => u.Roles)
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
        return users.Select(ToSummary).ToList();
    }

    private static UserSummary ToSummary(User u) =>
        new(u.Id, u.Username, u.DisplayName, u.IsActive, u.Roles.Select(r => r.RoleKey).ToList());
}
