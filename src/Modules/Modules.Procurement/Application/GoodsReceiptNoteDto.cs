namespace Modules.Procurement.Application;

public sealed record GrnLineDto(
    Guid Id, Guid PurchaseOrderLineId, Guid ItemId, decimal QuantityReceived, decimal UnitPrice, decimal LineValue);

public sealed record GoodsReceiptNoteDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid PurchaseOrderId,
    DateOnly ReceivedDate,
    decimal ReceivedValue,
    IReadOnlyList<GrnLineDto> Lines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateGoodsReceiptNoteLineRequest(Guid PurchaseOrderLineId, decimal QuantityReceived);

public sealed record CreateGoodsReceiptNoteRequest(
    Guid PurchaseOrderId, DateOnly ReceivedDate, IReadOnlyList<CreateGoodsReceiptNoteLineRequest> Lines);
