using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Infrastructure;

public sealed class EfAPInvoiceRepository : IAPInvoiceRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfAPInvoiceRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    public Task<APInvoice?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.APInvoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<APInvoice>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.APInvoices.AsNoTracking()
            .OrderByDescending(i => i.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.APInvoices.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<APInvoice>> ListByInvoiceDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        await _dbContext.APInvoices.AsNoTracking()
            .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end)
            .ToListAsync(cancellationToken);

    public void Add(APInvoice invoice) => _dbContext.APInvoices.Add(invoice);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
