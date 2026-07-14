namespace Modules.Procurement.Domain;

/// <summary>
/// One line of a <see cref="PurchaseRequisition"/> — a child entity, not an independent Business Object,
/// same "0..n child collection, only exists through its parent" pattern as
/// Modules.Finance.Domain.JournalLine. References an Item and a Cost Center — both by <see cref="Guid"/>
/// only, resolved and validated through <c>Modules.MasterData.Contracts.IItemLookup</c>/
/// <c>ICostCenterLookup</c> at the Application layer, never through a direct reference to
/// Modules.MasterData's own types (docs/architecture/01-architecture-foundation.md §3.2).
/// </summary>
public sealed class PurchaseRequisitionLine
{
    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid CostCenterId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal EstimatedUnitPrice { get; private set; }
    public string? LineDescription { get; private set; }

    public decimal EstimatedLineTotal => Quantity * EstimatedUnitPrice;

    internal PurchaseRequisitionLine(Guid itemId, Guid costCenterId, decimal quantity, decimal estimatedUnitPrice, string? lineDescription)
    {
        Id = Guid.NewGuid();
        ItemId = itemId;
        CostCenterId = costCenterId;
        Quantity = quantity;
        EstimatedUnitPrice = estimatedUnitPrice;
        LineDescription = lineDescription;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private PurchaseRequisitionLine()
    {
    }
}
