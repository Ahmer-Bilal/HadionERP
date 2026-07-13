namespace Platform.Security;

/// <summary>
/// Reference implementation of <see cref="ISecurityCatalog"/> backed by in-memory Role/Duty definitions.
/// Proves the Roles -> Duties -> Privileges resolution the same way
/// <see cref="Platform.Core.NumberRanges.InMemoryNumberRangeService"/> proved number ranges in Phase 0 —
/// a real deployment swaps this for a database-backed catalog behind the same interface without any
/// caller (AuthorizationService, module code) changing.
/// </summary>
public sealed class InMemorySecurityCatalog : ISecurityCatalog
{
    private readonly Dictionary<string, Role> _roles;
    private readonly Dictionary<string, Duty> _duties;

    public InMemorySecurityCatalog(IEnumerable<Role> roles, IEnumerable<Duty> duties)
    {
        _roles = roles.ToDictionary(r => r.Key);
        _duties = duties.ToDictionary(d => d.Key);
    }

    public IReadOnlyCollection<string> ResolveDutyKeys(IReadOnlyCollection<string> roleKeys)
    {
        return roleKeys
            .Where(_roles.ContainsKey)
            .SelectMany(key => _roles[key].DutyKeys)
            .Distinct()
            .ToList();
    }

    public IReadOnlyCollection<PrivilegeGrant> ResolveGrants(IReadOnlyCollection<string> roleKeys, string privilegeKey)
    {
        var dutyKeys = ResolveDutyKeys(roleKeys);

        return dutyKeys
            .Where(_duties.ContainsKey)
            .SelectMany(key => _duties[key].Grants)
            .Where(grant => grant.PrivilegeKey == privilegeKey)
            .ToList();
    }
}
