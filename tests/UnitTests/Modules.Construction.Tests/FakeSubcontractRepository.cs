using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Tests;

internal sealed class FakeSubcontractRepository : ISubcontractRepository
{
    private readonly Dictionary<Guid, Subcontract> _items = new();

    public Task<Subcontract?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<Subcontract>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Subcontract>>(
            _items.Values.OrderByDescending(s => s.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(Subcontract subcontract) => _items[subcontract.Id] = subcontract;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
