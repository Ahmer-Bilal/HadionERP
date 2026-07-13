using Platform.Core;

namespace Platform.Security;

/// <summary>
/// Reference implementation of <see cref="IAuthorizationService"/>. A principal is authorized for a
/// Privilege if ANY of their resolved grants for it either has no constraints, or has constraints all
/// satisfied by the resource context — matching the real-world rule that a user's overall permission is
/// the union of what their Roles/Duties grant (e.g. holding both "Approve Small POs" and "Approve Large
/// POs" means the larger limit applies, not the smaller).
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly ISecurityCatalog _catalog;

    public AuthorizationService(ISecurityCatalog catalog)
    {
        _catalog = catalog;
    }

    public AuthorizationResult Authorize(SecurityPrincipal principal, string privilegeKey, IReadOnlyDictionary<string, string>? resourceContext = null)
    {
        var grants = _catalog.ResolveGrants(principal.RoleKeys, privilegeKey);

        if (grants.Count == 0)
        {
            return AuthorizationResult.Deny(
                $"Principal '{principal.UserId}' holds no Duty granting '{privilegeKey}'.");
        }

        var satisfied = grants.Any(grant => AttributeConstraints.Satisfies(grant.Constraints, resourceContext));

        return satisfied
            ? AuthorizationResult.Allow()
            : AuthorizationResult.Deny(
                $"Principal '{principal.UserId}' holds '{privilegeKey}' but not for the given context " +
                "(e.g. the amount exceeds every grant's limit).");
    }
}
