using System.Security.Claims;

namespace Platform.Security;

/// <summary>
/// Mints and validates the bearer token a real login produces (`ARCHITECTURE-AUDIT.md` Part 1 §1) — kept
/// storage/module-agnostic here (no dependency on any module's <c>User</c> type, only a bare username and
/// role set) for the same "kernel defines the port, a module implements it" reason as
/// <see cref="IActorRoleAssignmentStore"/> and <c>Platform.Core.NumberRanges.INumberRangeService</c>. The
/// real implementation (JWT today) lives in <c>Modules.Identity.Infrastructure</c> — the one module with a
/// real database and the first real consumer of authentication.
/// </summary>
public interface ITokenService
{
    /// <summary>Issues a signed token for <paramref name="username"/>, valid for <paramref name="lifetime"/>
    /// from now. <paramref name="roleKeys"/> is embedded as claims so a request can be authorized without a
    /// second round-trip to resolve roles.</summary>
    (string Token, DateTimeOffset ExpiresAt) IssueToken(string username, IReadOnlyCollection<string> roleKeys, TimeSpan lifetime);

    /// <summary>Validates a bearer token and returns the claims principal it encodes, or null if the token
    /// is missing, malformed, expired, or fails signature verification.</summary>
    ClaimsPrincipal? ValidateToken(string token);
}
