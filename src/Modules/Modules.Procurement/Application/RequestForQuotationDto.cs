namespace Modules.Procurement.Application;

public sealed record RfqLineDto(Guid Id, Guid PurchaseRequisitionLineId, Guid ItemId, decimal Quantity);

public sealed record RfqInvitedVendorDto(Guid Id, Guid VendorId);

public sealed record RfqVendorQuoteLineDto(Guid Id, Guid VendorId, Guid RfqLineId, decimal QuotedUnitPrice);

public sealed record RequestForQuotationDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid PurchaseRequisitionId,
    string Description,
    DateOnly? ResponseDeadline,
    IReadOnlyList<RfqLineDto> Lines,
    IReadOnlyList<RfqInvitedVendorDto> InvitedVendors,
    IReadOnlyList<RfqVendorQuoteLineDto> VendorQuoteLines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateRequestForQuotationRequest(
    Guid PurchaseRequisitionId, string Description, IReadOnlyList<Guid> InvitedVendorIds, DateOnly? ResponseDeadline = null);

public sealed record RecordVendorQuoteRequest(Guid VendorId, Guid RfqLineId, decimal QuotedUnitPrice);
