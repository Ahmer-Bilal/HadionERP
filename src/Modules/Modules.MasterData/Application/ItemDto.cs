namespace Modules.MasterData.Application;

public sealed record ItemDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string ItemCode,
    string ItemName,
    string? ItemNameArabic,
    string ItemType,
    string UnitOfMeasure,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateItemRequest(
    string ItemCode,
    string ItemName,
    string ItemType,
    string UnitOfMeasure,
    string? ItemNameArabic = null);

public sealed record UpdateItemRequest(
    string ItemName,
    string UnitOfMeasure,
    string? ItemNameArabic = null,
    bool IsActive = true);
