namespace Platform.Security.Sod;

/// <summary>In-memory reference implementation of <see cref="ISodExceptionLog"/> — proves the mechanism
/// in Phase 0; a real deployment backs this with the persistent audit store instead.</summary>
public sealed class InMemorySodExceptionLog : ISodExceptionLog
{
    private readonly List<SodExceptionGrant> _grants = new();

    public void Grant(string userId, SodConflictRule rule, string approvedBy, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        _grants.Add(new SodExceptionGrant(userId, rule, approvedBy, reason, DateTimeOffset.UtcNow));
    }

    public bool IsGranted(string userId, SodConflictRule rule) =>
        _grants.Any(g => g.UserId == userId && g.Rule.Matches(rule.DutyKeyA, rule.DutyKeyB));

    public IReadOnlyCollection<SodExceptionGrant> History => _grants.AsReadOnly();
}
