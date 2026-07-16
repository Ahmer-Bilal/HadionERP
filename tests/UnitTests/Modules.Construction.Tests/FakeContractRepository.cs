using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Tests;

internal sealed class FakeContractRepository : IContractRepository
{
    private readonly Dictionary<Guid, Contract> _items = new();

    public Task<Contract?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<Contract>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Contract>>(
            _items.Values.OrderByDescending(c => c.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(Contract contract) => _items[contract.Id] = contract;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
