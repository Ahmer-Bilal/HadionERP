using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Tests;

internal sealed class FakeGoodsReceiptNoteRepository : IGoodsReceiptNoteRepository
{
    private readonly Dictionary<Guid, GoodsReceiptNote> _items = new();

    public Task<GoodsReceiptNote?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<GoodsReceiptNote>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GoodsReceiptNote>>(
            _items.Values.OrderByDescending(g => g.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public Task<IReadOnlyList<GoodsReceiptNote>> ListByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GoodsReceiptNote>>(
            _items.Values.Where(g => g.PurchaseOrderId == purchaseOrderId).ToList());

    public void Add(GoodsReceiptNote goodsReceiptNote) => _items[goodsReceiptNote.Id] = goodsReceiptNote;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
