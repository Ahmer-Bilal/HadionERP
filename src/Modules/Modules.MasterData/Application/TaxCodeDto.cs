namespace Modules.MasterData.Application;

public sealed record TaxCodeDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string TaxCodeCode,
    string TaxCodeName,
    string? TaxCodeNameArabic,
    decimal Rate,
    string TaxType,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateTaxCodeRequest(
    string TaxCodeCode,
    string TaxCodeName,
    decimal Rate,
    string TaxType,
    string? TaxCodeNameArabic = null);

public sealed record UpdateTaxCodeRequest(
    string TaxCodeName,
    decimal Rate,
    string? TaxCodeNameArabic = null,
    bool IsActive = true);
