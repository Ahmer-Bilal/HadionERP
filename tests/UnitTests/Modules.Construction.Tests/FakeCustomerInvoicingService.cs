using Modules.Finance.Contracts;

namespace Modules.Construction.Tests;

/// <summary>Records every raise call instead of actually creating an AR Invoice — IpcServiceTests only
/// needs to prove IpcService calls this correctly (right customer/amount/accounts), not re-test
/// ARInvoiceService's own validation, which Modules.Finance.Tests already covers.</summary>
internal sealed class FakeCustomerInvoicingService : ICustomerInvoicingService
{
    public List<RaiseCustomerInvoiceRequest> RaisedInvoices { get; } = new();

    public Task<Guid> RaiseInvoiceAsync(
        RaiseCustomerInvoiceRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RaisedInvoices.Add(request);
        return Task.FromResult(Guid.NewGuid());
    }
}
