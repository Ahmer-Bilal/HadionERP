using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

internal sealed class FakeARInvoiceRepository : IARInvoiceRepository
{
    private readonly Dictionary<Guid, ARInvoice> _items = new();

    public Task<ARInvoice?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<ARInvoice>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ARInvoice>>(
            _items.Values.OrderByDescending(i => i.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public Task<IReadOnlyList<ARInvoice>> ListByInvoiceDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ARInvoice>>(_items.Values.Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end).ToList());

    public void Add(ARInvoice invoice) => _items[invoice.Id] = invoice;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
