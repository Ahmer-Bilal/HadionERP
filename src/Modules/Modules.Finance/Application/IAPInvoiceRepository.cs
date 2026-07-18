using Modules.Finance.Domain;

namespace Modules.Finance.Application;

public interface IAPInvoiceRepository
{
    Task<APInvoice?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<APInvoice>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    /// <summary>Every invoice with <c>InvoiceDate</c> in range, any status — the real "AP transactions this
    /// period" list <c>ClosingActivityService</c> builds its Accounts Payable checklist step from (filtering
    /// to not-yet-Posted/Reversed is that caller's own business logic, not this query's concern).</summary>
    Task<IReadOnlyList<APInvoice>> ListByInvoiceDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(APInvoice invoice);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
