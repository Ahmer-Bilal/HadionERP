using Platform.Core;

namespace Modules.MasterData.Domain;

/// <summary>
/// A construction-industry business party — the first Master Data entity built
/// (docs/architecture/01-architecture-foundation.md #3.1) since every transactional module eventually
/// references it (Finance's AP/AR, Procurement's vendors, Construction's subcontractors/customers).
/// Follows the standard Business Object lifecycle (Platform.Core.BusinessObject): Draft (data entry) →
/// Submit → Approve (new-partner onboarding is a real fraud/compliance control point, per
/// docs/architecture/03-platform-services.md #2.2's Segregation of Duties example) → Approved is the
/// "active, usable" state master data settles into; there's no "Posted" concept for master data the way
/// there is for a financial document.
///
/// Owns three child collections — <see cref="Addresses"/>, <see cref="Contacts"/>, and
/// <see cref="BusinessRoles"/> — because a real company has several addresses by purpose (head office,
/// billing, shipping, one or more site offices), several contact people (a Procurement Manager, an
/// Accountant, a CEO, a Site Engineer), and (per docs/architecture/06-roadmap.md's Phase 2 design) several
/// simultaneous roles (a company can be both a Supplier and a Subcontractor) — not one shared field or a
/// single enum for the whole company. All three are child entities, not independent Business Objects.
///
/// <see cref="BusinessRoles"/> replaced the old single <see cref="Domain.PartnerType"/> enum once Phase 2
/// was actually reached — that rework was documented in the roadmap back at Phase 1 and deliberately
/// deferred rather than done twice.
/// </summary>
public sealed class BusinessPartner : BusinessObject
{
    private readonly List<BusinessPartnerAddress> _addresses = new();
    private readonly List<BusinessPartnerContact> _contacts = new();
    private readonly List<BusinessRole> _businessRoles = new();

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
    public string? TaxRegistrationNumber { get; private set; }

    public IReadOnlyCollection<BusinessPartnerAddress> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<BusinessPartnerContact> Contacts => _contacts.AsReadOnly();
    public IReadOnlyCollection<BusinessRole> BusinessRoles => _businessRoles.AsReadOnly();

    public BusinessPartner(string createdBy, string name, BusinessRoleType initialRole, string? trade = null) : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        AddBusinessRole(initialRole, trade);
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

    /// <summary>
    /// Adds a role. <paramref name="trade"/> is meaningless for Client/JointVenturePartner/
    /// GovernmentAuthority (no trade/specialty concept) but genuinely useful for the
    /// Supplier/Subcontractor/Consultant family, where the same partner can legitimately hold the same
    /// <see cref="BusinessRoleType"/> more than once with a different Trade (e.g. Subcontractor–Electrical
    /// and Subcontractor–Concrete on the same company) — docs/architecture/06-roadmap.md's Phase 2 design
    /// calls this out explicitly for Vendor Prequalification's own sake ("a vendor can be prequalified as a
    /// Steel Supplier without being prequalified as an Electrical Subcontractor").
    ///
    /// Government Authority is mutually exclusive with every other role — "no commercial relationship, no
    /// AP/AR posting, no scorecard" per the roadmap, so a partner is either a Government Authority or it
    /// participates in the commercial roles, never both.
    /// </summary>
    public BusinessRole AddBusinessRole(BusinessRoleType roleType, string? trade = null)
    {
        if (roleType == BusinessRoleType.GovernmentAuthority && _businessRoles.Count > 0)
            throw new InvalidOperationException("Government Authority cannot be combined with any other role.");
        if (roleType != BusinessRoleType.GovernmentAuthority && _businessRoles.Any(r => r.RoleType == BusinessRoleType.GovernmentAuthority))
            throw new InvalidOperationException("Government Authority cannot be combined with any other role.");
        if (_businessRoles.Any(r => r.RoleType == roleType && r.Trade == trade))
            throw new ArgumentException($"This partner already holds the {roleType} role with the same trade.");

        var role = new BusinessRole(roleType, trade);
        _businessRoles.Add(role);
        return role;
    }

    public void RemoveBusinessRole(Guid roleId) => _businessRoles.RemoveAll(r => r.Id == roleId);

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
