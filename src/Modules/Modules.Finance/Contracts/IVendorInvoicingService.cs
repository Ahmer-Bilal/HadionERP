namespace Modules.Finance.Contracts;

/// <summary>The AP mirror of <see cref="RaiseCustomerInvoiceRequest"/> — fields mirror
/// <c>Modules.Finance.Application.CreateAPInvoiceRequest</c> exactly.</summary>
public sealed record RaiseVendorInvoiceRequest(
    Guid VendorId,
    string VendorInvoiceNumber,
    DateOnly InvoiceDate,
    string Description,
    Guid ExpenseAccountId,
    Guid PayableAccountId,
    decimal NetAmount,
    Guid? CostCenterId,
    Guid? TaxCodeId,
    Guid? VatAccountId,
    // See RaiseCustomerInvoiceRequest's identical fields for the full reasoning.
    string SourceDocumentType,
    Guid SourceDocumentId);

/// <summary>
/// The AP mirror of <see cref="ICustomerInvoicingService"/> — Construction calls this when an IPC against a
/// Subcontract (never a Contract — that side bills the Customer via <see cref="ICustomerInvoicingService"/>)
/// is certified, raising a real AP Invoice in Draft against the subcontractor. Implemented in
/// Modules.Finance.Infrastructure, registered in Gateway.Api's DI container.
/// </summary>
public interface IVendorInvoicingService
{
    Task<Guid> RaiseInvoiceAsync(
        RaiseVendorInvoiceRequest request, string actor, string companyId, CancellationToken cancellationToken = default);
}
