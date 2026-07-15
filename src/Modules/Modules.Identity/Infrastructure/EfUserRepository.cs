using Microsoft.EntityFrameworkCore;
using Modules.Identity.Application;
using Modules.Identity.Domain;

namespace Modules.Identity.Infrastructure;

public sealed class EfUserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public EfUserRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Users.Include(u => u.Roles).AsNoTracking().OrderBy(u => u.Username).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Users.CountAsync(cancellationToken);

    public void Add(User user) => _dbContext.Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
