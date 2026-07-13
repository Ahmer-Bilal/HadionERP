namespace Platform.Security.Sod;

/// <summary>
/// A recorded, explicit exception allowing one user to hold both sides of an otherwise-conflicting
/// Duty pair — SoD conflicts are never silently bypassed, per docs/architecture/03-platform-services.md
/// #2.2 ("an explicit, logged exception"). This record is the log entry itself.
/// </summary>
public sealed record SodExceptionGrant(string UserId, SodConflictRule Rule, string ApprovedBy, string Reason, DateTimeOffset GrantedAt);
