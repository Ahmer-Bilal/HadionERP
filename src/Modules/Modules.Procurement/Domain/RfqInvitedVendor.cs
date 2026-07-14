namespace Modules.Procurement.Domain;

/// <summary>
/// One vendor invited to quote against a <see cref="RequestForQuotation"/> — a child entity, not an
/// independent Business Object. The invited-vendor set is fixed while the RFQ is being built (Draft) and
/// frozen once <see cref="RequestForQuotation.Submit"/> sends it out, same "frozen once submitted" rule as
/// every other module's line collections.
/// </summary>
public sealed class RfqInvitedVendor
{
    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }

    internal RfqInvitedVendor(Guid vendorId)
    {
        Id = Guid.NewGuid();
        VendorId = vendorId;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private RfqInvitedVendor()
    {
    }
}
