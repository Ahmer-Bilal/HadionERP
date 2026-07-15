namespace Modules.MasterData.Domain;

/// <summary>
/// One role a Business Partner holds — a child entity, not an independent Business Object, same "0..n
/// child collection, only exists through its parent" pattern as <see cref="BusinessPartnerAddress"/>.
/// Constructed only through <see cref="BusinessPartner.AddBusinessRole"/>.
///
/// <see cref="RoleType"/> is an admin-configurable lookup code (Lookup type <c>"BusinessRoleType"</c>, seeded
/// with Client/Supplier/Subcontractor/Consultant/JointVenturePartner/GovernmentAuthority/RentalCompany/
/// Manufacturer/ManpowerSupplier/TestingLaboratory) rather than a hardcoded enum — CLAUDE.md's explicit
/// "don't hard-code... look up data" instruction; an admin can add e.g. "InsuranceProvider" without a code
/// change. Validated against <c>LookupService</c> at the <c>BusinessPartnerService</c> layer, not here.
///
/// <see cref="Trade"/> is also lookup-backed data — but split into three separate Lookup Types, one per
/// role family (<c>"SubcontractorTrade"</c>, <c>"SupplierTrade"</c>, <c>"ConsultantTrade"</c>), not one
/// undifferentiated list: a Subcontractor's trades (Electrical/Concrete/Steel Structure/...) are a
/// genuinely different real-world taxonomy from a Supplier's (Steel/Cement/MEP Materials/...) or a
/// Consultant's (Structural/Architectural/MEP Design/...), per docs/architecture/06-roadmap.md's own Phase 2
/// design. Deliberately NOT enforced server-side (see this module's README Deferred section): it's a
/// suggested-values list surfaced to the UI, since trades vary by discipline and a hard validation here
/// would block a legitimate trade the seed list hasn't caught up with yet. Only meaningful for the
/// Supplier/Subcontractor/Consultant-family roles; null for roles like Client or Government Authority that
/// have no trade/specialty concept.
/// </summary>
public sealed class BusinessRole
{
    public Guid Id { get; private set; }
    public string RoleType { get; private set; }
    public string? Trade { get; private set; }

    internal BusinessRole(string roleType, string? trade)
    {
        Id = Guid.NewGuid();
        RoleType = roleType;
        Trade = trade;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private BusinessRole()
    {
        RoleType = null!;
    }
}
