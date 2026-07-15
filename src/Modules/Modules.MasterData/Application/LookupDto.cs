namespace Modules.MasterData.Application;

public sealed record LookupTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? NameArabic,
    bool IsSystemDefined,
    int ValueCount);

public sealed record LookupValueDto(
    Guid Id,
    string LookupTypeCode,
    string Code,
    string Name,
    string? NameArabic,
    bool IsActive,
    int SortOrder);

public sealed record CreateLookupTypeRequest(string Code, string Name, string? NameArabic = null);

public sealed record UpdateLookupTypeRequest(string Name, string? NameArabic = null);

public sealed record CreateLookupValueRequest(string Code, string Name, string? NameArabic = null, int SortOrder = 0);

public sealed record UpdateLookupValueRequest(string Name, string? NameArabic, int SortOrder);
