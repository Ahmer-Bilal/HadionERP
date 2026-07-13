namespace Platform.Workflow.Delegation;

/// <summary>
/// A temporary grant of one user's approval authority (for a specific Role) to another user — the
/// "delegation (out-of-office)" requirement in docs/architecture/03-platform-services.md #4. Covers the
/// date range inclusive of both ends.
/// </summary>
public sealed record Delegation(string FromUserId, string ToUserId, string RoleKey, DateOnly StartDate, DateOnly EndDate)
{
    public bool CoversDate(DateOnly date) => date >= StartDate && date <= EndDate;
}
