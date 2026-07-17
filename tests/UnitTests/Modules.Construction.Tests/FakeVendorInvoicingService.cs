using Modules.Finance.Contracts;

namespace Modules.Construction.Tests;

/// <summary>AP mirror of <c>FakeCustomerInvoicingService</c> — records raise calls instead of actually
/// creating an AP Invoice.</summary>
internal sealed class FakeVendorInvoicingService : IVendorInvoicingService
{
    public List<RaiseVendorInvoiceRequest> RaisedInvoices { get; } = new();

    public Task<Guid> RaiseInvoiceAsync(
        RaiseVendorInvoiceRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RaisedInvoices.Add(request);
        return Task.FromResult(Guid.NewGuid());
    }
}
