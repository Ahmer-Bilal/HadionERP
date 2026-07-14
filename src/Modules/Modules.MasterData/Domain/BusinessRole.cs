namespace Modules.MasterData.Domain;

/// <summary>
/// One role a Business Partner holds — a child entity, not an independent Business Object, same "0..n
/// child collection, only exists through its parent" pattern as <see cref="BusinessPartnerAddress"/>.
/// Constructed only through <see cref="BusinessPartner.AddBusinessRole"/>.
///
/// <see cref="Trade"/> is free text, not a hardcoded enum — docs/architecture/06-roadmap.md's Phase 2
/// design is explicit that trades/specialties (Subcontractor → Electrical/Concrete/Mechanical/...,
/// Supplier → Steel/Cement/MEP Materials/...) vary by discipline and grow over time, so they belong in
/// configuration (a suggested-values list surfaced to the UI, not enforced server-side yet — see this
/// module's README Deferred section), not compiled into the platform. Only meaningful for the
/// Supplier/Subcontractor/Consultant-family roles; null for roles like Client or Government Authority that
/// have no trade/specialty concept.
/// </summary>
public sealed class BusinessRole
{
    public Guid Id { get; private set; }
    public BusinessRoleType RoleType { get; private set; }
    public string? Trade { get; private set; }

    internal BusinessRole(BusinessRoleType roleType, string? trade)
    {
        Id = Guid.NewGuid();
        RoleType = roleType;
        Trade = trade;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private BusinessRole()
    {
    }
}
