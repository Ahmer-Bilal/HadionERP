using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Infrastructure;

public sealed class EfRequestForQuotationRepository : IRequestForQuotationRepository
{
    private readonly ProcurementDbContext _dbContext;

    public EfRequestForQuotationRepository(ProcurementDbContext dbContext) => _dbContext = dbContext;

    public Task<RequestForQuotation?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.RequestsForQuotation
            .Include(r => r.Lines).Include(r => r.InvitedVendors).Include(r => r.VendorQuoteLines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RequestForQuotation>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.RequestsForQuotation.AsNoTracking()
            .Include(r => r.Lines).Include(r => r.InvitedVendors).Include(r => r.VendorQuoteLines)
            .OrderByDescending(r => r.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.RequestsForQuotation.CountAsync(cancellationToken);

    public void Add(RequestForQuotation rfq) => _dbContext.RequestsForQuotation.Add(rfq);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
