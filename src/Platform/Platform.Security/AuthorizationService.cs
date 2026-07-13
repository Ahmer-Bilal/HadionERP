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

        var satisfied = grants.Any(grant => SatisfiesConstraints(grant.Constraints, resourceContext));

        return satisfied
            ? AuthorizationResult.Allow()
            : AuthorizationResult.Deny(
                $"Principal '{principal.UserId}' holds '{privilegeKey}' but not for the given context " +
                "(e.g. the amount exceeds every grant's limit).");
    }

    /// <summary>
    /// Constraint keys named "Max{Attribute}" are numeric upper-bound checks against
    /// resourceContext["{Attribute}"] (e.g. "MaxAmount" vs. resourceContext["Amount"]); any other
    /// constraint key is an exact-match check. An unconstrained grant (null/empty) always satisfies.
    /// </summary>
    private static bool SatisfiesConstraints(
        IReadOnlyDictionary<string, string>? constraints,
        IReadOnlyDictionary<string, string>? resourceContext)
    {
        if (constraints is null || constraints.Count == 0)
        {
            return true;
        }

        if (resourceContext is null)
        {
            return false;
        }

        foreach (var (constraintKey, constraintValue) in constraints)
        {
            if (constraintKey.StartsWith("Max", StringComparison.Ordinal))
            {
                var attributeName = constraintKey["Max".Length..];
                if (!resourceContext.TryGetValue(attributeName, out var actualRaw))
                {
                    return false;
                }

                if (!decimal.TryParse(constraintValue, out var max) || !decimal.TryParse(actualRaw, out var actual))
                {
                    return false;
                }

                if (actual > max)
                {
                    return false;
                }
            }
            else if (!resourceContext.TryGetValue(constraintKey, out var value) || value != constraintValue)
            {
                return false;
            }
        }

        return true;
    }
}
