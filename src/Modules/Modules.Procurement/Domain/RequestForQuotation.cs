using Platform.Core;

namespace Modules.Procurement.Domain;

/// <summary>
/// The second document in the procure-to-pay chain (docs/architecture/06-roadmap.md's "PR → RFQ → PO → GRN
/// → 3-way match") — invites a fixed set of vendors to quote a unit price against each line of an Approved
/// <see cref="PurchaseRequisition"/>. <see cref="Submit"/> is the point the RFQ is considered "sent" (the
/// invited-vendor set and line set both freeze); <see cref="RecordVendorQuote"/> then records each invited
/// vendor's price per line as quotes come back. <see cref="Approve"/> means the RFQ process is closed and
/// its recorded quotes are ready for a future Purchase Order to reference when selecting a vendor+price —
/// there is no separate "award" step on this entity itself, since picking the winning quote is a PO-creation
/// concern (task #102), not something the RFQ decides about itself.
/// </summary>
public sealed class RequestForQuotation : BusinessObject
{
    private readonly List<RfqLine> _lines = new();
    private readonly List<RfqInvitedVendor> _invitedVendors = new();
    private readonly List<RfqVendorQuoteLine> _vendorQuoteLines = new();

    public Guid PurchaseRequisitionId { get; private set; }

    public string Description { get; private set; }

    /// <summary>When quotes are due back from invited vendors — informational for now, same "carried but not
    /// enforced yet" treatment as <see cref="PurchaseRequisition.RequiredByDate"/>.</summary>
    public DateOnly? ResponseDeadline { get; private set; }

    public IReadOnlyCollection<RfqLine> Lines => _lines.AsReadOnly();

    public IReadOnlyCollection<RfqInvitedVendor> InvitedVendors => _invitedVendors.AsReadOnly();

    public IReadOnlyCollection<RfqVendorQuoteLine> VendorQuoteLines => _vendorQuoteLines.AsReadOnly();

    public RequestForQuotation(string createdBy, Guid purchaseRequisitionId, string description, DateOnly? responseDeadline = null)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        PurchaseRequisitionId = purchaseRequisitionId;
        Description = description;
        ResponseDeadline = responseDeadline;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private RequestForQuotation()
    {
        Description = null!;
    }

    /// <summary>Copies one line from the source Purchase Requisition — Item + Quantity only. Only while in
    /// Draft, same "frozen once submitted" rule as every other module's line collections.</summary>
    public RfqLine AddLine(Guid purchaseRequisitionLineId, Guid itemId, decimal quantity)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the RFQ is in Draft.");

        var line = new RfqLine(purchaseRequisitionLineId, itemId, quantity);
        _lines.Add(line);
        return line;
    }

    /// <summary>Invites one vendor to quote. Only while in Draft; rejects inviting the same vendor twice.</summary>
    public RfqInvitedVendor InviteVendor(Guid vendorId)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Vendors can only be invited while the RFQ is in Draft.");
        if (_invitedVendors.Any(v => v.VendorId == vendorId))
            throw new ArgumentException($"Vendor {vendorId} is already invited.", nameof(vendorId));

        var invited = new RfqInvitedVendor(vendorId);
        _invitedVendors.Add(invited);
        return invited;
    }

    /// <summary>Records one invited vendor's quoted unit price for one line — only once the RFQ has been
    /// sent (Submitted, so there is something to quote against and the invited-vendor set is frozen), only
    /// for a vendor that was actually invited and a line that actually belongs to this RFQ, and only once
    /// per (vendor, line) pair — a real quote revision is a future concern, not built here.</summary>
    public RfqVendorQuoteLine RecordVendorQuote(Guid vendorId, Guid rfqLineId, decimal quotedUnitPrice)
    {
        if (Status != BusinessObjectStatus.Submitted)
            throw new InvalidOperationException("Vendor quotes can only be recorded once the RFQ has been submitted.");
        if (!_invitedVendors.Any(v => v.VendorId == vendorId))
            throw new ArgumentException($"Vendor {vendorId} was not invited to this RFQ.", nameof(vendorId));
        if (!_lines.Any(l => l.Id == rfqLineId))
            throw new ArgumentException($"Line {rfqLineId} does not belong to this RFQ.", nameof(rfqLineId));
        if (quotedUnitPrice <= 0)
            throw new ArgumentException("Quoted unit price must be greater than zero.", nameof(quotedUnitPrice));
        if (_vendorQuoteLines.Any(q => q.VendorId == vendorId && q.RfqLineId == rfqLineId))
            throw new ArgumentException($"Vendor {vendorId} has already quoted line {rfqLineId}.");

        var quote = new RfqVendorQuoteLine(vendorId, rfqLineId, quotedUnitPrice);
        _vendorQuoteLines.Add(quote);
        return quote;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
