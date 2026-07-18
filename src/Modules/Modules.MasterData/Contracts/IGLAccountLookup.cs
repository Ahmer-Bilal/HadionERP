namespace Modules.MasterData.Contracts;

/// <summary>
/// The published, read-only view of a G/L Account another module (Finance) may depend on — per
/// docs/architecture/01-overview.md §3.2, a module may depend on another module's
/// published Contracts package only, never its Domain/Infrastructure/Application internals directly.
/// Deliberately a flat summary, not the real <c>GLAccount</c> aggregate — Finance needs to know "does this
/// account exist, is it postable, what's its normal balance," not the chart's own maintenance concerns
/// (parent hierarchy, lifecycle status).
/// </summary>
/// <param name="AccountType">The account's classical accounting type ("Asset"/"Liability"/"Equity"/
/// "Revenue"/"Expense") as a string, not the real Domain enum — same "flat summary, not the real aggregate"
/// reasoning as <see cref="NormalBalance"/> already being a derived string rather than the Domain type
/// itself. Lets a caller (e.g. Finance's Trial Balance report) classify/group accounts without a Domain
/// reference.</param>
/// <param name="ParentAccountId">Mirrors the chart hierarchy's <c>GLAccount.ParentAccountId</c> — null for a
/// top-level account. Lets a caller roll leaf-account balances up through header accounts without walking
/// the real aggregate.</param>
public sealed record GLAccountSummary(
    Guid Id,
    string AccountCode,
    string AccountName,
    string NormalBalance,
    bool IsPostable,
    bool IsActive,
    // Defaulted — every existing call site (posting-time validation, one account at a time) predates these
    // two fields and doesn't care about them; only a reporting caller (Trial Balance) that lists the whole
    // chart needs real values here, and does supply them.
    string AccountType = "Asset",
    Guid? ParentAccountId = null);

/// <summary>Read-only lookup Finance calls to validate a G/L Account reference before posting to it, and to
/// read the whole chart for reporting (e.g. Trial Balance). Implemented in Modules.MasterData.Infrastructure,
/// registered in Gateway.Api's DI container — Finance's Application layer depends on this interface only.</summary>
public interface IGLAccountLookup
{
    Task<GLAccountSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The whole chart of accounts, unfiltered — a financial report (Trial Balance, Income
    /// Statement, Balance Sheet) needs every account, including ones with no activity in the period, not
    /// just ones referenced by a specific document the way <see cref="GetAsync"/> is used elsewhere.</summary>
    Task<IReadOnlyList<GLAccountSummary>> ListAllAsync(CancellationToken cancellationToken = default);
}
