namespace Modules.Finance.Application;

/// <summary>One row of a Trial Balance — one G/L Account's Opening/Period-Activity/Ending Debit and Credit
/// for the requested period. Header (non-postable) accounts appear too, with their leaf descendants' amounts
/// rolled up, so the chart's grouping structure (docs/module/master-data.md's GLAccount hierarchy) reads the
/// same way in the report as it does in the Chart of Accounts screen itself.</summary>
public sealed record TrialBalanceAccountDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    /// <summary>0 for a top-level account, 1 for its direct children, and so on — lets the frontend indent
    /// the table to show the hierarchy without re-deriving it from ParentAccountId itself.</summary>
    int Level,
    bool IsHeader,
    decimal OpeningDebit,
    decimal OpeningCredit,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal EndingDebit,
    decimal EndingCredit);

/// <summary>The whole Trial Balance for one period — every G/L Account (leaf and header), plus the report's
/// own totals. <see cref="TotalEndingDebit"/> always equals <see cref="TotalEndingCredit"/> across leaf
/// accounts by the same double-entry identity <see cref="JournalEntry.IsBalanced"/> already enforces at
/// posting time; this report re-derives it from the posted ledger rather than trusting that invariant blindly,
/// so a real accounting discrepancy (e.g. a data-integrity bug) would show up here as a non-zero
/// <see cref="TotalEndingDebit"/> minus <see cref="TotalEndingCredit"/>.</summary>
public sealed record TrialBalanceDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<TrialBalanceAccountDto> Accounts,
    decimal TotalEndingDebit,
    decimal TotalEndingCredit);
