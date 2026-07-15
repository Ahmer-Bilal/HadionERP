using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Infrastructure;

public sealed class EfPurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly ProcurementDbContext _dbContext;

    public EfPurchaseOrderRepository(ProcurementDbContext dbContext) => _dbContext = dbContext;

    public Task<PurchaseOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PurchaseOrder>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.PurchaseOrders.AsNoTracking().Include(p => p.Lines)
            .OrderByDescending(p => p.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.PurchaseOrders.CountAsync(cancellationToken);

    public void Add(PurchaseOrder purchaseOrder) => _dbContext.PurchaseOrders.Add(purchaseOrder);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
