using Modules.Finance.Domain;

namespace Modules.Finance.Application;

public interface IARInvoiceRepository
{
    Task<ARInvoice?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ARInvoice>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    /// <summary>Same "InvoiceDate in range, any status" query as <c>IAPInvoiceRepository</c>'s own — see
    /// that interface's doc comment.</summary>
    Task<IReadOnlyList<ARInvoice>> ListByInvoiceDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(ARInvoice invoice);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
