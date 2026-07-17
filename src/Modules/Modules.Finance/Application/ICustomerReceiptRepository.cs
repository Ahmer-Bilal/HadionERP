using Modules.Finance.Domain;

namespace Modules.Finance.Application;

public interface ICustomerReceiptRepository
{
    Task<CustomerReceipt?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerReceipt>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Every receipt carrying an allocation against <paramref name="arInvoiceId"/>, regardless of
    /// status — mirrors <see cref="IPaymentRepository.ListByInvoiceAsync"/>.</summary>
    Task<IReadOnlyList<CustomerReceipt>> ListByInvoiceAsync(Guid arInvoiceId, CancellationToken cancellationToken = default);

    void Add(CustomerReceipt receipt);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
