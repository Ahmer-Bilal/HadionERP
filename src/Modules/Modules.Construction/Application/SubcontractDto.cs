namespace Modules.Construction.Application;

public sealed record SubcontractLineDto(
    Guid Id, string Code, string Description, string? DescriptionArabic, string UnitOfMeasure,
    decimal Quantity, decimal Rate, decimal Amount, Guid WbsElementId);

public sealed record BackChargeDto(Guid Id, string Description, decimal Amount, DateOnly DateIncurred);

public sealed record SubcontractDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid ProjectId,
    Guid? ContractId,
    Guid SubcontractorId,
    decimal? RetentionPercentage,
    decimal? MobilizationAdvancePercentage,
    int? DefectsLiabilityPeriodMonths,
    decimal SubcontractValue,
    decimal TotalBackCharges,
    decimal NetPayableValue,
    IReadOnlyList<SubcontractLineDto> Lines,
    IReadOnlyList<BackChargeDto> BackCharges,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateSubcontractLineRequest(
    string Code, string Description, string? DescriptionArabic, string UnitOfMeasure,
    decimal Quantity, decimal Rate, Guid WbsElementId);

public sealed record CreateSubcontractRequest(
    Guid ProjectId, Guid? ContractId, Guid SubcontractorId,
    decimal? RetentionPercentage, decimal? MobilizationAdvancePercentage, int? DefectsLiabilityPeriodMonths,
    IReadOnlyList<CreateSubcontractLineRequest> Lines);

public sealed record AddBackChargeRequest(string Description, decimal Amount, DateOnly DateIncurred);
