namespace Modules.Finance.Application;

/// <summary><see cref="OutstandingBalance"/> is simply Gross Amount once Posted — unlike
/// <c>APInvoiceDto.OutstandingBalance</c>, there's no Customer Receipt Business Object yet to reduce it
/// against, the AR-side mirror of the gap <c>Payment</c> closed for AP. Zero for a Draft/Submitted/Approved
/// invoice, same convention as AP.</summary>
public sealed record ARInvoiceDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid CustomerId,
    string? CustomerReference,
    DateOnly InvoiceDate,
    string Description,
    Guid RevenueAccountId,
    Guid ReceivableAccountId,
    Guid? CostCenterId,
    Guid? TaxCodeId,
    decimal TaxRate,
    Guid? VatAccountId,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount,
    decimal OutstandingBalance,
    Guid? LinkedJournalEntryId,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateARInvoiceRequest(
    Guid CustomerId,
    string? CustomerReference,
    DateOnly InvoiceDate,
    string Description,
    Guid RevenueAccountId,
    Guid ReceivableAccountId,
    decimal NetAmount,
    Guid? CostCenterId = null,
    Guid? TaxCodeId = null,
    Guid? VatAccountId = null);
