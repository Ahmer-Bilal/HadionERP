namespace Platform.Security;

/// <summary>The outcome of an authorization check, with a human-readable reason for denials (surfaced to
/// the user and written to the audit log — never a silent 403).</summary>
public sealed record AuthorizationResult(bool Allowed, string? Reason)
{
    public static AuthorizationResult Allow() => new(true, null);
    public static AuthorizationResult Deny(string reason) => new(false, reason);
}
