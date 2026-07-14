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
///
/// Owns two child collections — <see cref="Addresses"/> and <see cref="Contacts"/> — because a real
/// company has several addresses by purpose (head office, billing, shipping, one or more site offices)
/// and several contact people (a Procurement Manager, an Accountant, a CEO, a Site Engineer), each with
/// their own phone/email, not one shared pair of fields for the whole company. Both are child entities,
/// not independent Business Objects — see <see cref="BusinessPartnerAddress"/>/<see cref="BusinessPartnerContact"/>.
/// </summary>
public sealed class BusinessPartner : BusinessObject
{
    private readonly List<BusinessPartnerAddress> _addresses = new();
    private readonly List<BusinessPartnerContact> _contacts = new();

    public string Name { get; private set; }
    /// <summary>The partner's legal name in Arabic — required on a ZATCA-compliant tax invoice (the
    /// seller's Arabic name is a mandatory field, per ZATCA's e-invoicing implementation standards) and
    /// for a correctly localized Arabic UI, since <see cref="Name"/> alone is whatever script the partner
    /// was entered in (usually English/Latin) and Business Partner names are never auto-translated
    /// (docs/architecture — this platform's own hardcoded-Arabic guardrails exist precisely because UI
    /// *copy* must never be hardcoded; a real person's or company's actual legal name is data, not copy,
    /// and must be entered, not machine-translated). Optional at the domain level (a Draft partner may not
    /// have it yet) but should be required by the time a partner reaches Approved for any partner that
    /// will actually be invoiced — that validation isn't wired in yet, see Deferred in the module README.</summary>
    public string? NameArabic { get; private set; }
    public PartnerType PartnerType { get; private set; }
    public string? TaxRegistrationNumber { get; private set; }

    public IReadOnlyCollection<BusinessPartnerAddress> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<BusinessPartnerContact> Contacts => _contacts.AsReadOnly();

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

    public void UpdateTaxRegistrationNumber(string? taxRegistrationNumber) => TaxRegistrationNumber = taxRegistrationNumber;

    public void UpdateNameArabic(string? nameArabic) => NameArabic = nameArabic;

    /// <summary>
    /// Adds an address. Not gated by lifecycle status the way a financial document's fields would be (an
    /// approved vendor's address can be corrected or extended without "reversing" anything) — a
    /// deliberate difference from transactional Business Objects, not an oversight. Multiple addresses of
    /// the same <see cref="AddressType"/> are allowed on purpose (e.g. several Site Office addresses for
    /// different active projects).
    /// </summary>
    public BusinessPartnerAddress AddAddress(AddressType addressType, string? country, string? city, string? addressLine)
    {
        var address = new BusinessPartnerAddress(addressType, country, city, addressLine);
        _addresses.Add(address);
        return address;
    }

    public void RemoveAddress(Guid addressId) => _addresses.RemoveAll(a => a.Id == addressId);

    public BusinessPartnerContact AddContact(string name, string? jobTitle, string? email, string? phone)
    {
        var contact = new BusinessPartnerContact(name, jobTitle, email, phone);
        _contacts.Add(contact);
        return contact;
    }

    public void RemoveContact(Guid contactId) => _contacts.RemoveAll(c => c.Id == contactId);

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
