using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Infrastructure;

public sealed class EfCustomerReceiptRepository : ICustomerReceiptRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfCustomerReceiptRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    public Task<CustomerReceipt?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CustomerReceipts.Include(r => r.Allocations).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerReceipt>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.CustomerReceipts.Include(r => r.Allocations).AsNoTracking()
            .OrderByDescending(r => r.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.CustomerReceipts.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerReceipt>> ListByInvoiceAsync(Guid arInvoiceId, CancellationToken cancellationToken = default) =>
        await _dbContext.CustomerReceipts.Include(r => r.Allocations).AsNoTracking()
            .Where(r => r.Allocations.Any(a => a.ARInvoiceId == arInvoiceId))
            .ToListAsync(cancellationToken);

    public void Add(CustomerReceipt receipt) => _dbContext.CustomerReceipts.Add(receipt);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
