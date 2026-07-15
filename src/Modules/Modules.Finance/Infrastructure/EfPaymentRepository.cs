using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Infrastructure;

public sealed class EfPaymentRepository : IPaymentRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfPaymentRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    public Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Payments.Include(p => p.Allocations).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Payment>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.Payments.Include(p => p.Allocations).AsNoTracking()
            .OrderByDescending(p => p.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Payments.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Payment>> ListByInvoiceAsync(Guid apInvoiceId, CancellationToken cancellationToken = default) =>
        await _dbContext.Payments.Include(p => p.Allocations).AsNoTracking()
            .Where(p => p.Allocations.Any(a => a.APInvoiceId == apInvoiceId))
            .ToListAsync(cancellationToken);

    public void Add(Payment payment) => _dbContext.Payments.Add(payment);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
