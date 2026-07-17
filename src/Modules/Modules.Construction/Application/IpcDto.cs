namespace Modules.Construction.Application;

public sealed record IpcLineDto(
    Guid Id, Guid CommercialDocumentLineId, decimal Rate, decimal QuantityThisPeriod, decimal QuantityToDate,
    decimal ValueThisPeriod, decimal ValueToDate);

public sealed record IpcDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid ProjectId,
    string CommercialDocumentType,
    Guid CommercialDocumentId,
    Guid MeasurementSheetId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal? RetentionPercentageApplied,
    decimal? AdvancePaymentPercentageApplied,
    decimal OtherDeductions,
    Guid? RevenueAccountId,
    Guid? ReceivableAccountId,
    Guid? ExpenseAccountId,
    Guid? PayableAccountId,
    Guid? TaxCodeId,
    Guid? VatAccountId,
    Guid? LinkedArInvoiceId,
    Guid? LinkedApInvoiceId,
    decimal GrossValueToDate,
    decimal GrossValueThisPeriod,
    decimal GrossValuePreviousIpc,
    decimal RetentionAmount,
    decimal AdvanceRecoveryAmount,
    decimal NetPayable,
    IReadOnlyList<IpcLineDto> Lines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

/// <summary><see cref="RevenueAccountId"/>/<see cref="ReceivableAccountId"/> are required when
/// <see cref="CommercialDocumentType"/> is Contract (validated by <c>IpcService.CreateAsync</c>, since only
/// a Contract-type IPC ever raises an AR Invoice); <see cref="ExpenseAccountId"/>/<see cref="PayableAccountId"/>
/// are required when it's Subcontract (raises an AP Invoice instead) — exactly one pair is ever populated.
/// <see cref="TaxCodeId"/> requires <see cref="VatAccountId"/>, same rule both invoice services enforce.</summary>
public sealed record CreateIpcRequest(
    Guid ProjectId, string CommercialDocumentType, Guid CommercialDocumentId, Guid MeasurementSheetId,
    decimal OtherDeductions,
    Guid? RevenueAccountId = null, Guid? ReceivableAccountId = null, Guid? TaxCodeId = null, Guid? VatAccountId = null,
    Guid? ExpenseAccountId = null, Guid? PayableAccountId = null);
