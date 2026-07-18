using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

internal sealed class FakeAPInvoiceRepository : IAPInvoiceRepository
{
    private readonly Dictionary<Guid, APInvoice> _invoices = new();

    public Task<APInvoice?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_invoices.GetValueOrDefault(id));

    public Task<IReadOnlyList<APInvoice>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<APInvoice>>(
            _invoices.Values.OrderByDescending(i => i.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_invoices.Count);

    public Task<IReadOnlyList<APInvoice>> ListByInvoiceDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<APInvoice>>(_invoices.Values.Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end).ToList());

    public void Add(APInvoice invoice) => _invoices[invoice.Id] = invoice;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
