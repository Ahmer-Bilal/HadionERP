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

/// <summary>Metadata only — never the file bytes, which are fetched separately by id (see
/// <c>BusinessPartnersController.DownloadAttachment</c>) so listing attachments stays cheap.</summary>
public sealed record AttachmentDto(
    Guid Id, string FileName, string ContentType, long SizeBytes, string UploadedBy, DateTimeOffset UploadedAt);

public sealed record NoteDto(Guid Id, string Text, string CreatedBy, DateTimeOffset CreatedAt);

public sealed record AddBusinessPartnerNoteRequest(string Text);
