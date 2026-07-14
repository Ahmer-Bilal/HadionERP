namespace Modules.MasterData.Domain;

/// <summary>
/// One of a Business Partner's (possibly several) addresses — a child entity, not an independent
/// Business Object (docs/architecture/02-business-object-model.md #1's "Lines/Items" pattern: a 0..n
/// child collection that only exists through its parent, the same way a Purchase Order's lines do). No
/// document number, no lifecycle of its own — it's active exactly when its parent Business Partner is.
/// Constructed only through <see cref="BusinessPartner.AddAddress"/>, never directly, so the aggregate
/// root controls creation of its own children.
/// </summary>
public sealed class BusinessPartnerAddress
{
    public Guid Id { get; private set; }
    public AddressType AddressType { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? AddressLine { get; private set; }

    internal BusinessPartnerAddress(AddressType addressType, string? country, string? city, string? addressLine)
    {
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
    }
}
