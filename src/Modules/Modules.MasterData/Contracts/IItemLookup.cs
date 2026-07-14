namespace Modules.MasterData.Contracts;

/// <summary>
/// The published, read-only view of an Item another module (Procurement) may depend on — same
/// Contracts-package rule as <see cref="IGLAccountLookup"/>. Procurement needs to know "does this item
/// exist, is it active, what unit is its quantity in," not Item's own maintenance concerns (lifecycle
/// status transitions, Arabic name).
/// </summary>
public sealed record ItemSummary(
    Guid Id,
    string ItemCode,
    string ItemName,
    string UnitOfMeasure,
    bool IsActive);

/// <summary>Read-only lookup Procurement calls to validate an Item reference (e.g. on a Purchase
/// Requisition line) before adding it. Implemented in Modules.MasterData.Infrastructure, registered in
/// Gateway.Api's DI container.</summary>
public interface IItemLookup
{
    Task<ItemSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
