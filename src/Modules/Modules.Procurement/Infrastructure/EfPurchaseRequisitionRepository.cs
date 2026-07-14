using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Infrastructure;

public sealed class EfPurchaseRequisitionRepository : IPurchaseRequisitionRepository
{
    private readonly ProcurementDbContext _dbContext;

    public EfPurchaseRequisitionRepository(ProcurementDbContext dbContext) => _dbContext = dbContext;

    public Task<PurchaseRequisition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.PurchaseRequisitions.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PurchaseRequisition>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.PurchaseRequisitions.AsNoTracking().Include(r => r.Lines)
            .OrderByDescending(r => r.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.PurchaseRequisitions.CountAsync(cancellationToken);

    public void Add(PurchaseRequisition requisition) => _dbContext.PurchaseRequisitions.Add(requisition);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
