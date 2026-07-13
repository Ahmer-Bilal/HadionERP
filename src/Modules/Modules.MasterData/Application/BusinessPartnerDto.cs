namespace Modules.MasterData.Application;

public sealed record BusinessPartnerDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string Name,
    string PartnerType,
    string? TaxRegistrationNumber,
    string? Email,
    string? Phone,
    string? Country,
    string? City,
    string? AddressLine,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateBusinessPartnerRequest(
    string Name,
    string PartnerType,
    string? TaxRegistrationNumber,
    string? Email,
    string? Phone,
    string? Country,
    string? City,
    string? AddressLine);

public sealed record UpdateBusinessPartnerContactRequest(
    string? Email,
    string? Phone,
    string? Country,
    string? City,
    string? AddressLine);
