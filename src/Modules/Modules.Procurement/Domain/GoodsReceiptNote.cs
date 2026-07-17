using Platform.Core;

namespace Modules.Procurement.Domain;

/// <summary>
/// The fourth document in the procure-to-pay chain (ROADMAP.md's "PR → RFQ → PO → GRN →
/// 3-way match against AP") — records that goods against an Approved <see cref="PurchaseOrder"/> were
/// physically received. Multiple GRNs can exist against one PO (partial/staged deliveries are normal in
/// construction procurement); <see cref="GoodsReceiptNoteService.CreateAsync"/> enforces that the cumulative
/// quantity received across every non-Rejected GRN for a PO line never exceeds that line's ordered quantity.
/// Stops at Approved like every other procurement document so far — a GRN doesn't post a G/L entry itself
/// (inventory/GR-IR clearing accounting is out of scope for this phase, disclosed in the module README); it
/// exists so the 3-way match (<see cref="Application.ThreeWayMatchService"/>) has a real "received" figure
/// to compare Ordered/Invoiced against.
/// </summary>
public sealed class GoodsReceiptNote : BusinessObject
{
    private readonly List<GrnLine> _lines = new();

    public Guid PurchaseOrderId { get; private set; }

    public DateOnly ReceivedDate { get; private set; }

    public IReadOnlyCollection<GrnLine> Lines => _lines.AsReadOnly();

    /// <summary>Value of what was actually received, priced at each line's PO-committed unit price (copied
    /// onto <see cref="GrnLine.UnitPrice"/> at creation, same "freeze the financial fact" reasoning as
    /// <c>APInvoice.TaxRate</c>) — this is the "Received" figure the 3-way match compares against Ordered
    /// (<c>PurchaseOrder.Total</c>) and Invoiced (<c>Modules.Finance.Contracts.APInvoiceSummary.NetAmount</c>).</summary>
    public decimal ReceivedValue => _lines.Sum(l => l.LineValue);

    public GoodsReceiptNote(string createdBy, Guid purchaseOrderId, DateOnly receivedDate)
        : base(createdBy)
    {
        PurchaseOrderId = purchaseOrderId;
        ReceivedDate = receivedDate;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private GoodsReceiptNote()
    {
    }

    /// <summary>Adds one line — Item/UnitPrice copied from the referenced PO line (see
    /// <see cref="GrnLine"/>'s own doc comment). Only while in Draft, same "frozen once submitted" rule as
    /// every other module's line collections; the against-ordered-quantity check itself lives in the
    /// Service layer, which has visibility across every existing GRN for the PO — the Domain layer only
    /// enforces the strictly-positive-quantity invariant it can always verify on its own.</summary>
    public GrnLine AddLine(Guid purchaseOrderLineId, Guid itemId, decimal quantityReceived, decimal unitPrice)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the goods receipt note is in Draft.");
        if (quantityReceived <= 0)
            throw new ArgumentException("Quantity received must be greater than zero.", nameof(quantityReceived));

        var line = new GrnLine(purchaseOrderLineId, itemId, quantityReceived, unitPrice);
        _lines.Add(line);
        return line;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
