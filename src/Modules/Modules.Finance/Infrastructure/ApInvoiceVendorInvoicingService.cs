using Modules.Finance.Application;
using Modules.Finance.Contracts;

namespace Modules.Finance.Infrastructure;

/// <summary>Implements <see cref="IVendorInvoicingService"/> by delegating into
/// <see cref="APInvoiceService.CreateAsync"/> — the AP mirror of
/// <see cref="ArInvoiceCustomerInvoicingService"/>, same "thin adapter, not a reimplementation" reasoning.</summary>
public sealed class ApInvoiceVendorInvoicingService : IVendorInvoicingService
{
    private readonly APInvoiceService _apInvoiceService;

    public ApInvoiceVendorInvoicingService(APInvoiceService apInvoiceService) => _apInvoiceService = apInvoiceService;

    public async Task<Guid> RaiseInvoiceAsync(
        RaiseVendorInvoiceRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        var created = await _apInvoiceService.CreateAsync(
            new CreateAPInvoiceRequest(
                request.VendorId, request.VendorInvoiceNumber, request.InvoiceDate, request.Description,
                request.ExpenseAccountId, request.PayableAccountId, request.NetAmount,
                request.CostCenterId, request.TaxCodeId, request.VatAccountId),
            actor, companyId, cancellationToken,
            sourceDocumentType: request.SourceDocumentType, sourceDocumentId: request.SourceDocumentId);

        return created.Id;
    }
}
