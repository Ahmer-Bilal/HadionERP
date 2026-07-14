using Modules.Finance.Domain;

namespace Modules.Finance.Application;

public interface IAPInvoiceRepository
{
    Task<APInvoice?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<APInvoice>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(APInvoice invoice);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
