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
    decimal GrossValueToDate,
    decimal GrossValueThisPeriod,
    decimal GrossValuePreviousIpc,
    decimal RetentionAmount,
    decimal AdvanceRecoveryAmount,
    decimal NetPayable,
    IReadOnlyList<IpcLineDto> Lines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateIpcRequest(
    Guid ProjectId, string CommercialDocumentType, Guid CommercialDocumentId, Guid MeasurementSheetId,
    decimal OtherDeductions);
