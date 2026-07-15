using Modules.Finance.Contracts;

namespace Modules.Procurement.Tests;

internal sealed class FakeAPInvoiceLookup : IAPInvoiceLookup
{
    private readonly Dictionary<Guid, APInvoiceSummary> _invoices = new();

    public void Add(APInvoiceSummary invoice) => _invoices[invoice.Id] = invoice;

    public Task<APInvoiceSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_invoices.GetValueOrDefault(id));
}
