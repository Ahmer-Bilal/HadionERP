namespace Modules.Finance.Application;

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
    Guid? LinkedJournalEntryId,
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
    Guid? VatAccountId = null);
