using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Infrastructure;

public sealed class EfGLAccountRepository : IGLAccountRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfGLAccountRepository(MasterDataDbContext dbContext) => _dbContext = dbContext;

    // Tracked (not AsNoTracking): the Update/Submit/Approve paths load via GetAsync then mutate + Save,
    // so EF must observe the entity for changes. GetByCode (a read-only uniqueness check) and List stay
    // AsNoTracking.
    public Task<GLAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.GLAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<GLAccount?> GetByCodeAsync(string accountCode, CancellationToken cancellationToken = default) =>
        _dbContext.GLAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountCode == accountCode, cancellationToken);

    public async Task<IReadOnlyList<GLAccount>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.GLAccounts.AsNoTracking().OrderBy(a => a.AccountCode).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.GLAccounts.CountAsync(cancellationToken);

    public void Add(GLAccount account) => _dbContext.GLAccounts.Add(account);

    public void Remove(GLAccount account) => _dbContext.GLAccounts.Remove(account);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
