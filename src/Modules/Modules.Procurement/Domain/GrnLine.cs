namespace Modules.Procurement.Domain;

/// <summary>
/// One line of a <see cref="GoodsReceiptNote"/> — a child entity, not an independent Business Object, same
/// "0..n child collection, only exists through its parent" pattern as <see cref="PurchaseOrderLine"/>. Item
/// and <see cref="UnitPrice"/> are copied from the referenced <see cref="PurchaseOrderLine"/> at GRN creation
/// time (frozen, not a live reference) — the receipt should value goods at the price they were actually
/// ordered at, never at whatever the PO line happens to say later.
/// </summary>
public sealed class GrnLine
{
    public Guid Id { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal QuantityReceived { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineValue => QuantityReceived * UnitPrice;

    internal GrnLine(Guid purchaseOrderLineId, Guid itemId, decimal quantityReceived, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        PurchaseOrderLineId = purchaseOrderLineId;
        ItemId = itemId;
        QuantityReceived = quantityReceived;
        UnitPrice = unitPrice;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private GrnLine()
    {
    }
}
