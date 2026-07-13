namespace Platform.Security;

/// <summary>
/// The authenticated identity behind a request — what Platform.Security's engines check against. Built
/// from whatever identity provider authenticated the user (real SSO/OIDC integration is deferred to the
/// API/Gateway layer, see README.md); this type is the stable shape everything downstream depends on so
/// swapping the identity provider later never touches authorization logic.
///
/// <paramref name="ScopeAttributes"/> carries row-level scoping dimensions (docs/architecture/03-platform-services.md #2.3)
/// such as "CompanyId" -> {"C001", "C002"}. A dimension key that is absent entirely means the principal
/// is unrestricted on that dimension (e.g. a platform administrator); a key that is present restricts
/// access to exactly the listed values. This is the same "absent = unrestricted, present = allow-list"
/// convention used throughout the row-level security check — see RowLevel/RowLevelSecurityService.cs.
/// </summary>
public sealed record SecurityPrincipal(
    string UserId,
    IReadOnlyCollection<string> RoleKeys,
    IReadOnlyDictionary<string, IReadOnlySet<string>> ScopeAttributes);
