namespace Platform.Security;

/// <summary>
/// The single entry point every module calls to check "is this user allowed to do this" — the hybrid
/// RBAC + ABAC check from docs/architecture/04-platform-services.md #2.2. Modules never re-implement
/// permission checks themselves; they call this with the Privilege key their action requires.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks whether <paramref name="principal"/> holds a Duty granting <paramref name="privilegeKey"/>.
    /// <paramref name="resourceContext"/> carries the attributes of the specific thing being acted on
    /// (e.g. {"Amount": "45000"} for a PO approval) — required only when the grant is attribute-
    /// constrained (e.g. "up to 50,000 SAR"); pass null for unconstrained checks.
    /// </summary>
    AuthorizationResult Authorize(SecurityPrincipal principal, string privilegeKey, IReadOnlyDictionary<string, string>? resourceContext = null);
}
