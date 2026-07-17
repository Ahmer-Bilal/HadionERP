namespace Modules.Construction.Application;

public sealed record VariationOrderLineDto(
    Guid Id, Guid? CommercialDocumentLineId, string? Code, string? Description, string? UnitOfMeasure,
    Guid? WbsElementId, decimal QuantityDelta, decimal Rate, decimal Amount);

public sealed record VariationOrderDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid ProjectId,
    string CommercialDocumentType,
    Guid CommercialDocumentId,
    string Reason,
    decimal TotalValue,
    IReadOnlyList<VariationOrderLineDto> Lines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

/// <summary>Exactly one of <see cref="CommercialDocumentLineId"/> (an adjustment to an existing line) or
/// <see cref="Code"/>/<see cref="Description"/>/<see cref="UnitOfMeasure"/>/<see cref="WbsElementId"/> (a
/// wholly new line) must be supplied — validated in <c>VariationOrderService.CreateAsync</c>.</summary>
public sealed record CreateVariationOrderLineRequest(
    Guid? CommercialDocumentLineId, decimal QuantityDelta,
    string? Code = null, string? Description = null, string? DescriptionArabic = null,
    string? UnitOfMeasure = null, Guid? WbsElementId = null, decimal? Rate = null);

public sealed record CreateVariationOrderRequest(
    Guid ProjectId, string CommercialDocumentType, Guid CommercialDocumentId, string Reason,
    IReadOnlyList<CreateVariationOrderLineRequest> Lines);
