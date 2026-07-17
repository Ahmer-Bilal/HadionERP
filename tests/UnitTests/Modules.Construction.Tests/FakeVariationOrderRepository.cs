using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Tests;

internal sealed class FakeVariationOrderRepository : IVariationOrderRepository
{
    private readonly Dictionary<Guid, VariationOrder> _items = new();

    public Task<VariationOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<VariationOrder>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<VariationOrder>>(
            _items.Values.OrderByDescending(o => o.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(VariationOrder variationOrder) => _items[variationOrder.Id] = variationOrder;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
