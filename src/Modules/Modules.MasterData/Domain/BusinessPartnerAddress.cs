namespace Modules.MasterData.Domain;

/// <summary>
/// One of a Business Partner's (possibly several) addresses — a child entity, not an independent
/// Business Object (docs/architecture/02-business-object-model.md #1's "Lines/Items" pattern: a 0..n
/// child collection that only exists through its parent, the same way a Purchase Order's lines do). No
/// document number, no lifecycle of its own — it's active exactly when its parent Business Partner is.
/// Constructed only through <see cref="BusinessPartner.AddAddress"/>, never directly, so the aggregate
/// root controls creation of its own children.
///
/// <see cref="AddressType"/> and <see cref="Country"/> are both admin-configurable lookup codes (Lookup
/// types <c>"AddressType"</c> and <c>"Country"</c>) rather than a hardcoded enum/free text — validated
/// against <c>LookupService</c> at the <c>BusinessPartnerService</c> layer, not here.
/// </summary>
public sealed class BusinessPartnerAddress
{
    public Guid Id { get; private set; }
    public string AddressType { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? AddressLine { get; private set; }

    internal BusinessPartnerAddress(string addressType, string? country, string? city, string? addressLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(addressType);
        Id = Guid.NewGuid();
        AddressType = addressType;
        Country = country;
        City = city;
        AddressLine = addressLine;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private BusinessPartnerAddress()
    {
        AddressType = null!;
    }
}
