namespace Modules.Construction.Application;

public sealed record BoqLineDto(
    Guid Id, string Code, string Description, string? DescriptionArabic, string UnitOfMeasure,
    decimal Quantity, decimal Rate, decimal Amount, Guid WbsElementId);

public sealed record ContractDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid ProjectId,
    string ContractType,
    string? PaymentTerms,
    decimal? AdvancePaymentPercentage,
    int? DefectsLiabilityPeriodMonths,
    decimal ContractValue,
    IReadOnlyList<BoqLineDto> BoqLines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateBoqLineRequest(
    string Code, string Description, string? DescriptionArabic, string UnitOfMeasure,
    decimal Quantity, decimal Rate, Guid WbsElementId);

public sealed record CreateContractRequest(
    Guid ProjectId, string ContractType, string? PaymentTerms,
    decimal? AdvancePaymentPercentage, int? DefectsLiabilityPeriodMonths,
    IReadOnlyList<CreateBoqLineRequest> BoqLines);
