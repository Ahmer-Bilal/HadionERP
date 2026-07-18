using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Infrastructure;

public sealed class EfBankAccountRepository : IBankAccountRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfBankAccountRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    public Task<BankAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.BankAccounts.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<BankAccount?> GetByCodeAsync(string accountCode, CancellationToken cancellationToken = default) =>
        _dbContext.BankAccounts.AsNoTracking().FirstOrDefaultAsync(b => b.AccountCode == accountCode, cancellationToken);

    public async Task<IReadOnlyList<BankAccount>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.BankAccounts.AsNoTracking()
            .OrderBy(b => b.AccountCode).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.BankAccounts.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<BankAccount>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.BankAccounts.AsNoTracking()
            .Where(b => b.IsActive).OrderBy(b => b.AccountCode).ToListAsync(cancellationToken);

    public void Add(BankAccount bankAccount) => _dbContext.BankAccounts.Add(bankAccount);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
