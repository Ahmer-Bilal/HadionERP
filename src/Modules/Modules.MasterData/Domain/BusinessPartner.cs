using Platform.Core;

namespace Modules.MasterData.Domain;

/// <summary>
/// A customer, vendor, or both — the first Master Data entity built (docs/architecture/01-architecture-foundation.md
/// #3.1) since every transactional module eventually references it (Finance's AP/AR, Procurement's
/// vendors, Construction's subcontractors/customers). Follows the standard Business Object lifecycle
/// (Platform.Core.BusinessObject): Draft (data entry) → Submit → Approve (new-partner onboarding is a
/// real fraud/compliance control point, per docs/architecture/03-platform-services.md #2.2's Segregation
/// of Duties example) → Approved is the "active, usable" state master data settles into; there's no
/// "Posted" concept for master data the way there is for a financial document.
/// </summary>
public sealed class BusinessPartner : BusinessObject
{
    public string Name { get; private set; }
    public PartnerType PartnerType { get; private set; }
    public string? TaxRegistrationNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? AddressLine { get; private set; }

    public BusinessPartner(string createdBy, string name, PartnerType partnerType) : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        PartnerType = partnerType;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private BusinessPartner()
    {
        Name = null!;
    }

    /// <summary>
    /// Master data edits are not gated by lifecycle status the way a financial document's would be (an
    /// approved vendor's phone number can be corrected without "reversing" anything) — this is a
    /// deliberate difference from transactional Business Objects, not an oversight.
    /// </summary>
    public void UpdateContactDetails(string? email, string? phone, string? country, string? city, string? addressLine)
    {
        Email = email;
        Phone = phone;
        Country = country;
        City = city;
        AddressLine = addressLine;
    }

    public void UpdateTaxRegistrationNumber(string? taxRegistrationNumber) => TaxRegistrationNumber = taxRegistrationNumber;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
