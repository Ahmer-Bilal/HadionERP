namespace Modules.Procurement.Application;

public sealed record PurchaseRequisitionLineDto(
    Guid Id, Guid ItemId, Guid CostCenterId, decimal Quantity, decimal EstimatedUnitPrice,
    decimal EstimatedLineTotal, string? LineDescription);

public sealed record PurchaseRequisitionDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string Description,
    DateOnly? RequiredByDate,
    decimal EstimatedTotal,
    IReadOnlyList<PurchaseRequisitionLineDto> Lines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreatePurchaseRequisitionLineRequest(
    Guid ItemId, Guid CostCenterId, decimal Quantity, decimal EstimatedUnitPrice, string? LineDescription = null);

public sealed record CreatePurchaseRequisitionRequest(
    string Description, IReadOnlyList<CreatePurchaseRequisitionLineRequest> Lines, DateOnly? RequiredByDate = null);
