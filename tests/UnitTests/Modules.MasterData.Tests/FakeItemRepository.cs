using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Tests;

internal sealed class FakeItemRepository : IItemRepository
{
    private readonly Dictionary<Guid, Item> _items = new();

    public Task<Item?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<Item?> GetByCodeAsync(string itemCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Values.FirstOrDefault(i => i.ItemCode == itemCode));

    public Task<IReadOnlyList<Item>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Item>>(
            _items.Values.OrderBy(i => i.ItemCode).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(Item item) => _items[item.Id] = item;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
