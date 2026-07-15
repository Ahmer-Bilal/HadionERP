using Platform.Core;

namespace Modules.Procurement.Domain;

/// <summary>
/// The third document in the procure-to-pay chain (docs/architecture/06-roadmap.md's "PR → RFQ → PO → GRN →
/// 3-way match against AP") — a commitment to buy specific Items in specific quantities from one Vendor at a
/// negotiated (not estimated) unit price. Built "from an RFQ-selected quote or direct" (task #102's own
/// wording): <see cref="RequestForQuotationId"/> is set when the lines/prices were copied from one vendor's
/// recorded quotes on an Approved <see cref="RequestForQuotation"/> (kept purely for traceability, the same
/// role <c>RfqLine.PurchaseRequisitionLineId</c> plays back to the PR); it is null for a direct PO raised
/// with no RFQ behind it. Stops at Approved, like every procurement document so far — GRN (task #103, not
/// built yet) is the actual financial receipt event; a PO itself is a commitment, not a posting.
/// </summary>
public sealed class PurchaseOrder : BusinessObject
{
    private readonly List<PurchaseOrderLine> _lines = new();

    public Guid VendorId { get; private set; }

    public Guid? RequestForQuotationId { get; private set; }

    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(l => l.LineTotal);

    public PurchaseOrder(string createdBy, Guid vendorId, Guid? requestForQuotationId = null)
        : base(createdBy)
    {
        VendorId = vendorId;
        RequestForQuotationId = requestForQuotationId;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private PurchaseOrder()
    {
    }

    /// <summary>Adds one line at a negotiated unit price — unlike <c>PurchaseRequisitionLine.EstimatedUnitPrice</c>,
    /// this is the real committed price, whether copied from a winning RFQ quote or entered directly. Lines
    /// can only be added while in Draft, same "frozen once submitted" rule as every other module's line
    /// collections.</summary>
    public PurchaseOrderLine AddLine(
        Guid itemId, Guid costCenterId, decimal quantity, decimal unitPrice, Guid? rfqLineId = null)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the purchase order is in Draft.");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice <= 0)
            throw new ArgumentException("Unit price must be greater than zero.", nameof(unitPrice));

        var line = new PurchaseOrderLine(itemId, costCenterId, quantity, unitPrice, rfqLineId);
        _lines.Add(line);
        return line;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
