namespace Modules.Finance.Application;

/// <summary><see cref="OutstandingBalance"/> is Gross Amount minus every Posted-and-unreversed
/// <c>Payment</c> allocation against this invoice — see <c>APInvoiceService.GetOutstandingBalanceAsync</c>.
/// Zero for a Draft/Submitted/Approved invoice (nothing to pay against until it's Posted) and equal to
/// Gross Amount until any real payment has posted.</summary>
public sealed record APInvoiceDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid VendorId,
    string VendorInvoiceNumber,
    DateOnly InvoiceDate,
    string Description,
    Guid ExpenseAccountId,
    Guid PayableAccountId,
    Guid? CostCenterId,
    Guid? TaxCodeId,
    decimal TaxRate,
    Guid? VatAccountId,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount,
    decimal OutstandingBalance,
    Guid? LinkedJournalEntryId,
    string? SourceDocumentType,
    Guid? SourceDocumentId,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateAPInvoiceRequest(
    Guid VendorId,
    string VendorInvoiceNumber,
    DateOnly InvoiceDate,
    string Description,
    Guid ExpenseAccountId,
    Guid PayableAccountId,
    decimal NetAmount,
    Guid? CostCenterId = null,
    Guid? TaxCodeId = null,
    Guid? VatAccountId = null,
    // Optional — a real procurement-driven vendor invoice references the Purchase Order it's matched
    // against (3-way match territory); an AP clerk picks this from a dropdown when keying an invoice
    // directly. Null means Manual (no PO involved).
    Guid? PurchaseOrderId = null);
