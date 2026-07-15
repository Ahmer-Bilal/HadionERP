using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Infrastructure;

public sealed class EfGoodsReceiptNoteRepository : IGoodsReceiptNoteRepository
{
    private readonly ProcurementDbContext _dbContext;

    public EfGoodsReceiptNoteRepository(ProcurementDbContext dbContext) => _dbContext = dbContext;

    public Task<GoodsReceiptNote?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.GoodsReceiptNotes.Include(g => g.Lines).FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GoodsReceiptNote>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.GoodsReceiptNotes.AsNoTracking().Include(g => g.Lines)
            .OrderByDescending(g => g.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.GoodsReceiptNotes.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<GoodsReceiptNote>> ListByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) =>
        await _dbContext.GoodsReceiptNotes.AsNoTracking().Include(g => g.Lines)
            .Where(g => g.PurchaseOrderId == purchaseOrderId).ToListAsync(cancellationToken);

    public void Add(GoodsReceiptNote goodsReceiptNote) => _dbContext.GoodsReceiptNotes.Add(goodsReceiptNote);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
