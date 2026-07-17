using Platform.Core;

namespace Modules.MasterData.Domain;

/// <summary>
/// A construction-industry business party — the first Master Data entity built
/// (docs/architecture/01-overview.md #3.1) since every transactional module eventually
/// references it (Finance's AP/AR, Procurement's vendors, Construction's subcontractors/customers).
/// Follows the standard Business Object lifecycle (Platform.Core.BusinessObject): Draft (data entry) →
/// Submit → Approve (new-partner onboarding is a real fraud/compliance control point, per
/// docs/architecture/04-platform-services.md #2.2's Segregation of Duties example) → Approved is the
/// "active, usable" state master data settles into; there's no "Posted" concept for master data the way
/// there is for a financial document.
///
/// Owns three child collections — <see cref="Addresses"/>, <see cref="Contacts"/>, and
/// <see cref="BusinessRoles"/> — because a real company has several addresses by purpose (head office,
/// billing, shipping, one or more site offices), several contact people (a Procurement Manager, an
/// Accountant, a CEO, a Site Engineer), and (per ROADMAP.md's Phase 2 design) several
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

    public BusinessPartner(string createdBy, string name, string initialRole, string? trade = null) : base(createdBy)
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
    /// the same <paramref name="addressType"/> are allowed on purpose (e.g. several Site Office addresses
    /// for different active projects). Both <paramref name="addressType"/> and <paramref name="country"/>
    /// are admin-configurable lookup codes (Lookup types <c>"AddressType"</c>/<c>"Country"</c>) — validated
    /// against <c>LookupService</c> at the <c>BusinessPartnerService</c> layer, not here (the Domain layer
    /// has no dependency on the lookup store, same "Domain stays pure, Application validates references"
    /// split every other cross-cutting reference in this codebase follows).
    /// </summary>
    public BusinessPartnerAddress AddAddress(string addressType, string? country, string? city, string? addressLine)
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

    /// <summary>The one <see cref="BusinessRoleType"/> lookup value every consumer across every module
    /// (this Domain, Procurement's VendorPrequalificationService/RequestForQuotationService, Finance's
    /// APInvoiceService) special-cases by this exact code — "no commercial relationship, no AP/AR posting,
    /// no scorecard" per ROADMAP.md's Phase 2 design. Making <see cref="BusinessRole.RoleType"/>
    /// admin-extensible (any other code) doesn't change this one rule; a real SAP/Dynamics account-group
    /// taxonomy has the same kind of fixed special case (e.g. a "one-time customer" account group) sitting
    /// alongside otherwise-configurable groups.</summary>
    public const string GovernmentAuthorityRoleCode = "GovernmentAuthority";

    /// <summary>
    /// Adds a role. <paramref name="trade"/> is meaningless for Client/JointVenturePartner/
    /// GovernmentAuthority (no trade/specialty concept) but genuinely useful for the
    /// Supplier/Subcontractor/Consultant family, where the same partner can legitimately hold the same
    /// role type more than once with a different Trade (e.g. Subcontractor–Electrical and
    /// Subcontractor–Concrete on the same company) — ROADMAP.md's Phase 2 design calls
    /// this out explicitly for Vendor Prequalification's own sake ("a vendor can be prequalified as a Steel
    /// Supplier without being prequalified as an Electrical Subcontractor"). <paramref name="roleType"/> is
    /// an admin-configurable lookup code (Lookup type <c>"BusinessRoleType"</c>) — validated against
    /// <c>LookupService</c> at the <c>BusinessPartnerService</c> layer, same split as <see cref="AddAddress"/>.
    ///
    /// Government Authority is mutually exclusive with every other role — see
    /// <see cref="GovernmentAuthorityRoleCode"/>.
    /// </summary>
    public BusinessRole AddBusinessRole(string roleType, string? trade = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleType);
        if (roleType == GovernmentAuthorityRoleCode && _businessRoles.Count > 0)
            throw new InvalidOperationException("Government Authority cannot be combined with any other role.");
        if (roleType != GovernmentAuthorityRoleCode && _businessRoles.Any(r => r.RoleType == GovernmentAuthorityRoleCode))
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
