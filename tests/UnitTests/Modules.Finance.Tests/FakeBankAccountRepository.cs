using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

internal sealed class FakeBankAccountRepository : IBankAccountRepository
{
    private readonly Dictionary<Guid, BankAccount> _accounts = new();

    public Task<BankAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.GetValueOrDefault(id));

    public Task<BankAccount?> GetByCodeAsync(string accountCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.Values.FirstOrDefault(a => a.AccountCode == accountCode));

    public Task<IReadOnlyList<BankAccount>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BankAccount>>(
            _accounts.Values.OrderBy(a => a.AccountCode).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_accounts.Count);

    public Task<IReadOnlyList<BankAccount>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BankAccount>>(_accounts.Values.Where(a => a.IsActive).OrderBy(a => a.AccountCode).ToList());

    public void Add(BankAccount bankAccount) => _accounts[bankAccount.Id] = bankAccount;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
