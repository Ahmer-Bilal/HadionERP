namespace Platform.Security.Sod;

/// <summary>
/// Append-only record of Segregation of Duties exceptions — mirrors the audit principle in
/// docs/architecture/04-platform-services.md #5 (nothing is silently overridden; overrides are logged
/// facts). A real deployment persists this to the same immutable audit store Platform.Audit writes to.
/// </summary>
public interface ISodExceptionLog
{
    void Grant(string userId, SodConflictRule rule, string approvedBy, string reason);
    bool IsGranted(string userId, SodConflictRule rule);
    IReadOnlyCollection<SodExceptionGrant> History { get; }
}
