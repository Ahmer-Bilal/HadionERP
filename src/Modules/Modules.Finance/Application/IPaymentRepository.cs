using Modules.Finance.Domain;

namespace Modules.Finance.Application;

public interface IPaymentRepository
{
    Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Every Payment carrying an allocation against <paramref name="apInvoiceId"/>, regardless of
    /// status — the caller (typically the outstanding-balance computation) decides which statuses actually
    /// count. Powers both the overpayment check on Post and the "Payments applied" list an AP Invoice's
    /// own detail page shows.</summary>
    Task<IReadOnlyList<Payment>> ListByInvoiceAsync(Guid apInvoiceId, CancellationToken cancellationToken = default);

    void Add(Payment payment);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
