namespace Modules.Construction.Application;

public sealed record MeasurementLineDto(
    Guid Id, Guid CommercialDocumentLineId, decimal QuantitySubmitted, decimal? QuantityCertified, string? Remarks);

public sealed record MeasurementSheetDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid ProjectId,
    string CommercialDocumentType,
    Guid CommercialDocumentId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Notes,
    IReadOnlyList<MeasurementLineDto> Lines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateMeasurementLineRequest(Guid CommercialDocumentLineId, decimal QuantitySubmitted, string? Remarks);

public sealed record CreateMeasurementSheetRequest(
    Guid ProjectId, string CommercialDocumentType, Guid CommercialDocumentId,
    DateOnly PeriodStart, DateOnly PeriodEnd, string? Notes,
    IReadOnlyList<CreateMeasurementLineRequest> Lines);

public sealed record CertifyMeasurementLineRequest(Guid LineId, decimal QuantityCertified);

public sealed record CertifyMeasurementSheetRequest(IReadOnlyList<CertifyMeasurementLineRequest> Lines);
