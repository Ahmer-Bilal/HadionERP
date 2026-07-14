using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Contracts;

namespace Modules.MasterData.Infrastructure;

/// <summary>Implements the published <see cref="IGLAccountLookup"/> contract by projecting straight off
/// this module's own <see cref="MasterDataDbContext"/> — Finance depends on the interface only (see
/// Modules.MasterData.Contracts), never on this class or the DbContext directly.</summary>
public sealed class EfGLAccountLookup : IGLAccountLookup
{
    private readonly MasterDataDbContext _dbContext;

    public EfGLAccountLookup(MasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<GLAccountSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.GLAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return account is null
            ? null
            : new GLAccountSummary(account.Id, account.AccountCode, account.AccountName, account.NormalBalance, account.IsPostable, account.IsActive);
    }
}
