namespace Modules.Procurement.Application;

public sealed record PurchaseOrderLineDto(
    Guid Id, Guid ItemId, Guid CostCenterId, decimal Quantity, decimal UnitPrice, decimal LineTotal, Guid? RfqLineId);

public sealed record PurchaseOrderDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid VendorId,
    Guid? RequestForQuotationId,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreatePurchaseOrderLineRequest(Guid ItemId, Guid CostCenterId, decimal Quantity, decimal UnitPrice);

/// <summary>
/// Two mutually exclusive creation shapes, matching task #102's "from an RFQ-selected quote or direct"
/// wording: when <see cref="RequestForQuotationId"/> is set, <see cref="Lines"/> must be null/empty — the
/// server builds the PO's lines itself from that Approved RFQ's recorded quotes for <see cref="VendorId"/>
/// (which must be one of the RFQ's invited vendors, and must have quoted every line). When
/// <see cref="RequestForQuotationId"/> is null, <see cref="Lines"/> carries the direct entry.
/// </summary>
public sealed record CreatePurchaseOrderRequest(
    Guid VendorId, Guid? RequestForQuotationId, IReadOnlyList<CreatePurchaseOrderLineRequest>? Lines);
