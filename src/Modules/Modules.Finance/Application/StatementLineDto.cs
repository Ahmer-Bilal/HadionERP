namespace Modules.Finance.Application;

/// <summary>One line of a financial statement (Income Statement, Balance Sheet) — an account's net movement
/// or balance for the period, plus an optional comparison-period figure and its variance. Shared shape
/// between both statements since both are "classify TrialBalanceService's accounts by type, net them to
/// their normal balance side" — see <see cref="IncomeStatementService"/>/<see cref="BalanceSheetService"/>'s
/// own doc comments.</summary>
/// <param name="AccountId">Null for a synthetic line with no backing G/L Account — currently only
/// <see cref="BalanceSheetDto"/>'s computed "Retained Earnings (Undistributed)" equity line, since there is
/// no Period Closing Center yet to post it to a real Retained Earnings account
/// (see UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md). The frontend supplies its own translated label whenever
/// this is null rather than the backend embedding an English display string.</param>
public sealed record StatementLineDto(
    Guid? AccountId,
    string AccountCode,
    string AccountName,
    decimal Amount,
    decimal? CompareAmount,
    decimal? Variance,
    decimal? VariancePercent);
