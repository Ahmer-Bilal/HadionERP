namespace Modules.MasterData.Application;

public sealed record BusinessPartnerAddressDto(Guid Id, string AddressType, string? Country, string? City, string? AddressLine);

public sealed record BusinessPartnerContactDto(Guid Id, string Name, string? JobTitle, string? Email, string? Phone);

public sealed record BusinessPartnerDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string Name,
    string PartnerType,
    string? TaxRegistrationNumber,
    IReadOnlyList<BusinessPartnerAddressDto> Addresses,
    IReadOnlyList<BusinessPartnerContactDto> Contacts,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateBusinessPartnerRequest(string Name, string PartnerType, string? TaxRegistrationNumber);

public sealed record AddBusinessPartnerAddressRequest(string AddressType, string? Country, string? City, string? AddressLine);

public sealed record AddBusinessPartnerContactRequest(string Name, string? JobTitle, string? Email, string? Phone);
