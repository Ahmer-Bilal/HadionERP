namespace Modules.Procurement.Domain;

/// <summary>
/// One line of a <see cref="RequestForQuotation"/> — a child entity, not an independent Business Object.
/// Copied from the source <see cref="PurchaseRequisition"/>'s own lines at RFQ creation time (Item + Quantity
/// only — an RFQ asks "what price for this quantity of this item," it doesn't re-carry the PR's estimated
/// price or cost center), with <see cref="PurchaseRequisitionLineId"/> kept purely for traceability back to
/// the line it originated from.
/// </summary>
public sealed class RfqLine
{
    public Guid Id { get; private set; }
    public Guid PurchaseRequisitionLineId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }

    internal RfqLine(Guid purchaseRequisitionLineId, Guid itemId, decimal quantity)
    {
        Id = Guid.NewGuid();
        PurchaseRequisitionLineId = purchaseRequisitionLineId;
        ItemId = itemId;
        Quantity = quantity;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private RfqLine()
    {
    }
}
