using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Tests;

internal sealed class FakePurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly Dictionary<Guid, PurchaseOrder> _items = new();

    public Task<PurchaseOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<PurchaseOrder>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PurchaseOrder>>(
            _items.Values.OrderByDescending(p => p.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(PurchaseOrder purchaseOrder) => _items[purchaseOrder.Id] = purchaseOrder;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
