namespace Modules.MasterData.Contracts;

/// <summary>
/// The published, read-only view of a G/L Account another module (Finance) may depend on — per
/// docs/architecture/01-architecture-foundation.md §3.2, a module may depend on another module's
/// published Contracts package only, never its Domain/Infrastructure/Application internals directly.
/// Deliberately a flat summary, not the real <c>GLAccount</c> aggregate — Finance needs to know "does this
/// account exist, is it postable, what's its normal balance," not the chart's own maintenance concerns
/// (parent hierarchy, lifecycle status).
/// </summary>
public sealed record GLAccountSummary(
    Guid Id,
    string AccountCode,
    string AccountName,
    string NormalBalance,
    bool IsPostable,
    bool IsActive);

/// <summary>Read-only lookup Finance calls to validate a G/L Account reference before posting to it.
/// Implemented in Modules.MasterData.Infrastructure, registered in Gateway.Api's DI container — Finance's
/// Application layer depends on this interface only.</summary>
public interface IGLAccountLookup
{
    Task<GLAccountSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
