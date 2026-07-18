using Modules.Finance.Application;
using Modules.Finance.Contracts;

namespace Modules.Finance.Infrastructure;

/// <summary>
/// Implements <see cref="ICustomerInvoicingService"/> by delegating straight into
/// <see cref="ARInvoiceService.CreateAsync"/> — a thin adapter, not a reimplementation, so every validation
/// rule (customer must hold the Client role and be Approved, accounts must be active/postable, a Tax Code
/// requires a VAT account) lives in exactly one place. Lives in Infrastructure rather than Application, same
/// as <see cref="PassThroughBudgetCheckService"/> and <see cref="EfAPInvoiceLookup"/> — this module's own
/// convention for "the concrete thing a consumer shouldn't see," regardless of whether it touches a
/// DbContext directly.
/// </summary>
public sealed class ArInvoiceCustomerInvoicingService : ICustomerInvoicingService
{
    private readonly ARInvoiceService _arInvoiceService;

    public ArInvoiceCustomerInvoicingService(ARInvoiceService arInvoiceService) => _arInvoiceService = arInvoiceService;

    public async Task<Guid> RaiseInvoiceAsync(
        RaiseCustomerInvoiceRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        var created = await _arInvoiceService.CreateAsync(
            new CreateARInvoiceRequest(
                request.CustomerId, request.CustomerReference, request.InvoiceDate, request.Description,
                request.RevenueAccountId, request.ReceivableAccountId, request.NetAmount,
                request.CostCenterId, request.TaxCodeId, request.VatAccountId),
            actor, companyId, cancellationToken,
            sourceDocumentType: request.SourceDocumentType, sourceDocumentId: request.SourceDocumentId);

        return created.Id;
    }
}
