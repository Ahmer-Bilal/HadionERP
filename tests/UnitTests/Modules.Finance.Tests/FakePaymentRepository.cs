using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

internal sealed class FakePaymentRepository : IPaymentRepository
{
    private readonly Dictionary<Guid, Payment> _payments = new();

    public Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_payments.GetValueOrDefault(id));

    public Task<IReadOnlyList<Payment>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Payment>>(
            _payments.Values.OrderByDescending(p => p.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_payments.Count);

    public Task<IReadOnlyList<Payment>> ListByInvoiceAsync(Guid apInvoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Payment>>(
            _payments.Values.Where(p => p.Allocations.Any(a => a.APInvoiceId == apInvoiceId)).ToList());

    public void Add(Payment payment) => _payments[payment.Id] = payment;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
