using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Infrastructure;

public sealed class EfARInvoiceRepository : IARInvoiceRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfARInvoiceRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    public Task<ARInvoice?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.ARInvoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ARInvoice>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.ARInvoices.AsNoTracking()
            .OrderByDescending(i => i.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.ARInvoices.CountAsync(cancellationToken);

    public void Add(ARInvoice invoice) => _dbContext.ARInvoices.Add(invoice);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
