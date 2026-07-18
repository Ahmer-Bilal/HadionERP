using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Tests;

internal sealed class FakeGLAccountRepository : IGLAccountRepository
{
    private readonly Dictionary<Guid, GLAccount> _accounts = new();

    public Task<GLAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.GetValueOrDefault(id));

    public Task<GLAccount?> GetByCodeAsync(string accountCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.Values.FirstOrDefault(a => a.AccountCode == accountCode));

    public Task<IReadOnlyList<GLAccount>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GLAccount>>(
            _accounts.Values.OrderBy(a => a.AccountCode).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_accounts.Count);

    public void Add(GLAccount account) => _accounts[account.Id] = account;

    public void Remove(GLAccount account) => _accounts.Remove(account.Id);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
