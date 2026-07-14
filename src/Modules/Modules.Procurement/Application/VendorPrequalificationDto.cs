namespace Modules.Procurement.Application;

public sealed record VendorPrequalificationDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid BusinessPartnerId,
    string RoleType,
    string? Trade,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateVendorPrequalificationRequest(Guid BusinessPartnerId, string RoleType, string? Trade = null);

/// <summary>Metadata only — never the file bytes, same "fetch separately by id" shape as
/// Modules.MasterData.Application.AttachmentDto.</summary>
public sealed record AttachmentDto(
    Guid Id, string FileName, string ContentType, long SizeBytes, string UploadedBy, DateTimeOffset UploadedAt);
