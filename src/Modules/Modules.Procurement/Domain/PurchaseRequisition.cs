using Platform.Core;

namespace Modules.Procurement.Domain;

/// <summary>
/// The first document in the procure-to-pay chain (ROADMAP.md's Phase 2: "PR → RFQ →
/// PO → GRN → 3-way match against AP") — a cost-center owner's request to buy specific Items in specific
/// quantities, at an estimated (not yet negotiated) price. Stops at Approved, like every Master-Data-ish BO
/// so far (GLAccount/Item/CostCenter/TaxCode/VendorPrequalification) — a requisition is an internal request,
/// not a financial document, so there is no Post/Reverse; a future RFQ references an Approved
/// PurchaseRequisition's lines, it doesn't turn the PR itself into a ledger posting.
///
/// <see cref="EstimatedTotal"/> is computed, never stored — same "computed, not persisted" choice as
/// <c>Modules.Finance.Domain.JournalEntry.TotalDebits</c>: it's a preview for the requester/approver, not a
/// number a future price negotiation is bound by (the RFQ/PO slices will carry the real negotiated amounts).
/// </summary>
public sealed class PurchaseRequisition : BusinessObject
{
    private readonly List<PurchaseRequisitionLine> _lines = new();

    public string Description { get; private set; }

    /// <summary>When the requester needs the items by — informational for now (no lead-time/scheduling
    /// logic reads it yet), but real enough to carry on every requisition since a real PR always states one.</summary>
    public DateOnly? RequiredByDate { get; private set; }

    /// <summary>The lines making up this requisition — 0..n child collection, only exists through this
    /// parent, same pattern as <c>Modules.Finance.Domain.JournalEntry.Lines</c>.</summary>
    public IReadOnlyCollection<PurchaseRequisitionLine> Lines => _lines.AsReadOnly();

    public decimal EstimatedTotal => _lines.Sum(l => l.EstimatedLineTotal);

    public PurchaseRequisition(string createdBy, string description, DateOnly? requiredByDate = null)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Description = description;
        RequiredByDate = requiredByDate;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private PurchaseRequisition()
    {
        Description = null!;
    }

    /// <summary>Adds one line. Quantity and estimated unit price must both be strictly positive — a
    /// zero-quantity or free line has nothing to requisition. Lines can only be added while in Draft, same
    /// "frozen once submitted" rule as <c>JournalEntry.AddLine</c> (changing a line after submission would
    /// silently invalidate whatever approval was already given against the original estimate).</summary>
    public PurchaseRequisitionLine AddLine(
        Guid itemId, Guid costCenterId, decimal quantity, decimal estimatedUnitPrice, string? lineDescription = null)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the purchase requisition is in Draft.");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (estimatedUnitPrice <= 0)
            throw new ArgumentException("Estimated unit price must be greater than zero.", nameof(estimatedUnitPrice));

        var line = new PurchaseRequisitionLine(itemId, costCenterId, quantity, estimatedUnitPrice, lineDescription);
        _lines.Add(line);
        return line;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
