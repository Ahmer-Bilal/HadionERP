namespace Platform.Security;

/// <summary>
/// Resolves a bare actor-id string (what every module currently passes around as "actor", e.g.
/// "system/ui" — see README.md's "Deferred" section: real SSO/OIDC needs an actual identity provider and
/// deployment target, neither of which exists yet) to the Role keys assigned to them, so authorization
/// and workflow-eligibility checks have real <see cref="SecurityPrincipal"/> data to check against instead
/// of an unconditional grant. A real deployment replaces the in-memory implementation of this with role
/// assignment resolved from the authenticated identity, behind this same interface — no calling code
/// changes, the same "swap the reference implementation" pattern used everywhere else in the kernel.
/// </summary>
public interface IActorRoleAssignmentStore
{
    IReadOnlyCollection<string> ResolveRoleKeys(string actorId);
}
