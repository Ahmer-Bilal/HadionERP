namespace Modules.MasterData.Application;

public sealed record CostCenterDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string CostCenterCode,
    string CostCenterName,
    string? CostCenterNameArabic,
    Guid? ParentCostCenterId,
    bool IsPostable,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateCostCenterRequest(
    string CostCenterCode,
    string CostCenterName,
    string? CostCenterNameArabic = null,
    Guid? ParentCostCenterId = null,
    bool IsPostable = true);

public sealed record UpdateCostCenterRequest(
    string CostCenterName,
    string? CostCenterNameArabic = null,
    Guid? ParentCostCenterId = null,
    bool IsPostable = true,
    bool IsActive = true);
