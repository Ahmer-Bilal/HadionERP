namespace Platform.Security;

/// <summary>
/// The registered set of Roles/Duties/Privileges for a tenant, and the resolution from a principal's
/// assigned Role keys down to its effective Privilege grants. In production this catalog is populated
/// from configuration (docs/architecture/04-data-and-api.md #3), not code — security administrators
/// maintain Roles/Duties through an admin UI, they are not hard-coded per module.
/// </summary>
public interface ISecurityCatalog
{
    IReadOnlyCollection<PrivilegeGrant> ResolveGrants(IReadOnlyCollection<string> roleKeys, string privilegeKey);

    /// <summary>All Duty keys a set of Roles resolves to — the input to Segregation of Duties checks.</summary>
    IReadOnlyCollection<string> ResolveDutyKeys(IReadOnlyCollection<string> roleKeys);
}
