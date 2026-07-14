using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Infrastructure;

public sealed class EfVendorPrequalificationRepository : IVendorPrequalificationRepository
{
    private readonly ProcurementDbContext _dbContext;

    public EfVendorPrequalificationRepository(ProcurementDbContext dbContext) => _dbContext = dbContext;

    public Task<VendorPrequalification?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.VendorPrequalifications.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VendorPrequalification>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.VendorPrequalifications.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.VendorPrequalifications.CountAsync(cancellationToken);

    public void Add(VendorPrequalification prequalification) => _dbContext.VendorPrequalifications.Add(prequalification);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
