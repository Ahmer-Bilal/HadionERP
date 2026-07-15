using Modules.Procurement.Domain;

namespace Modules.Procurement.Application;

public interface IGoodsReceiptNoteRepository
{
    Task<GoodsReceiptNote?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoodsReceiptNote>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Every GRN raised against one PO, any status — used both to enforce "cumulative received
    /// quantity per PO line never exceeds ordered quantity" at creation time (counts every non-Rejected GRN)
    /// and to compute the 3-way match's "Received" figure (counts only Approved GRNs).</summary>
    Task<IReadOnlyList<GoodsReceiptNote>> ListByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);

    void Add(GoodsReceiptNote goodsReceiptNote);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
