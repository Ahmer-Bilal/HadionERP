namespace Modules.Procurement.Domain;

/// <summary>
/// One invited vendor's quoted unit price against one <see cref="RfqLine"/> — a child entity, not an
/// independent Business Object. Recorded after the RFQ has been sent (Submitted) — a quote can't exist
/// before there's anything to quote against, and the invited-vendor set is already frozen by then.
/// </summary>
public sealed class RfqVendorQuoteLine
{
    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }
    public Guid RfqLineId { get; private set; }
    public decimal QuotedUnitPrice { get; private set; }

    internal RfqVendorQuoteLine(Guid vendorId, Guid rfqLineId, decimal quotedUnitPrice)
    {
        Id = Guid.NewGuid();
        VendorId = vendorId;
        RfqLineId = rfqLineId;
        QuotedUnitPrice = quotedUnitPrice;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private RfqVendorQuoteLine()
    {
    }
}
