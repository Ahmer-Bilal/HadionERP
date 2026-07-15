namespace Modules.Procurement.Domain;

/// <summary>
/// One line of a <see cref="PurchaseOrder"/> — a child entity, not an independent Business Object, same
/// "0..n child collection, only exists through its parent" pattern as <see cref="RfqLine"/>/
/// <see cref="PurchaseRequisitionLine"/>. Carries a Cost Center (unlike <see cref="RfqLine"/>, which drops
/// it — a PO needs one back for the Finance budget-check call) and the real negotiated
/// <see cref="UnitPrice"/>. <see cref="RfqLineId"/> is set only when this line was copied from a winning RFQ
/// vendor quote, purely for traceability back to that quote.
/// </summary>
public sealed class PurchaseOrderLine
{
    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid CostCenterId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Guid? RfqLineId { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    internal PurchaseOrderLine(Guid itemId, Guid costCenterId, decimal quantity, decimal unitPrice, Guid? rfqLineId)
    {
        Id = Guid.NewGuid();
        ItemId = itemId;
        CostCenterId = costCenterId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        RfqLineId = rfqLineId;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private PurchaseOrderLine()
    {
    }
}
