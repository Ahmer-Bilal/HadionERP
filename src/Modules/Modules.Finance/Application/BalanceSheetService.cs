namespace Modules.Finance.Application;

/// <summary>
/// The Balance Sheet report — reuses <see cref="TrialBalanceService"/> the same way
/// <see cref="IncomeStatementService"/> does (see its own doc comment for the dependency reasoning), asking
/// it for cumulative Ending balances from the beginning of the ledger (<see cref="DateOnly.MinValue"/>)
/// through the requested as-of date, then netting each Asset/Liability/Equity account to its normal balance
/// side.
///
/// The one thing this report computes that Trial Balance doesn't: since this platform has no Period Closing
/// Center yet (see UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md) to post a formal closing entry moving
/// cumulative Revenue/Expense into a real Retained Earnings G/L account, this service computes that amount
/// itself — cumulative Revenue minus cumulative Expense through the as-of date — and appends it to
/// <see cref="BalanceSheetDto.EquityLines"/> as a line with no backing account (<c>AccountId: null</c>).
/// This isn't a fabricated number: it's the exact amount the fundamental accounting equation
/// (Assets = Liabilities + Equity) requires to make <see cref="BalanceSheetDto.TotalAssets"/> equal
/// <see cref="BalanceSheetDto.TotalLiabilities"/> + <see cref="BalanceSheetDto.TotalEquity"/>, true for any
/// balanced set of postings regardless of whether a closing entry ever ran.
/// </summary>
public sealed class BalanceSheetService
{
    private readonly TrialBalanceService _trialBalanceService;

    public BalanceSheetService(TrialBalanceService trialBalanceService) => _trialBalanceService = trialBalanceService;

    public async Task<BalanceSheetDto> GetAsync(DateOnly asOfDate, DateOnly? compareAsOfDate, CancellationToken cancellationToken = default)
    {
        var current = await _trialBalanceService.GetAsync(DateOnly.MinValue, asOfDate, cancellationToken);
        var compare = compareAsOfDate is { } compareDate
            ? await _trialBalanceService.GetAsync(DateOnly.MinValue, compareDate, cancellationToken)
            : null;
        var compareByAccount = compare?.Accounts.ToDictionary(a => a.AccountId)
            ?? new Dictionary<Guid, TrialBalanceAccountDto>();

        static decimal NetDebitNormal(TrialBalanceAccountDto a) => a.EndingDebit - a.EndingCredit;
        static decimal NetCreditNormal(TrialBalanceAccountDto a) => a.EndingCredit - a.EndingDebit;

        List<StatementLineDto> BuildLines(string accountType, Func<TrialBalanceAccountDto, decimal> netOf) =>
            current.Accounts
                .Where(a => !a.IsHeader && a.AccountType == accountType)
                .Select(a =>
                {
                    var amount = netOf(a);
                    decimal? compareAmount = compareByAccount.TryGetValue(a.AccountId, out var c) ? netOf(c) : null;
                    var variance = compareAmount.HasValue ? amount - compareAmount.Value : (decimal?)null;
                    var variancePercent = variance.HasValue && compareAmount != 0
                        ? Math.Round(variance.Value / Math.Abs(compareAmount!.Value) * 100m, 2)
                        : (decimal?)null;
                    return new StatementLineDto(a.AccountId, a.AccountCode, a.AccountName, amount, compareAmount, variance, variancePercent);
                })
                .OrderBy(l => l.AccountCode, StringComparer.Ordinal)
                .ToList();

        var assetLines = BuildLines("Asset", NetDebitNormal);
        var liabilityLines = BuildLines("Liability", NetCreditNormal);
        var equityLines = BuildLines("Equity", NetCreditNormal);

        static decimal RetainedEarnings(TrialBalanceDto tb) =>
            tb.Accounts.Where(a => !a.IsHeader && a.AccountType == "Revenue").Sum(NetCreditNormal)
            - tb.Accounts.Where(a => !a.IsHeader && a.AccountType == "Expense").Sum(NetDebitNormal);

        var retainedEarnings = RetainedEarnings(current);
        decimal? compareRetainedEarnings = compare is null ? null : RetainedEarnings(compare);
        var retainedEarningsVariance = compareRetainedEarnings.HasValue ? retainedEarnings - compareRetainedEarnings.Value : (decimal?)null;
        var retainedEarningsVariancePercent = retainedEarningsVariance.HasValue && compareRetainedEarnings != 0
            ? Math.Round(retainedEarningsVariance.Value / Math.Abs(compareRetainedEarnings!.Value) * 100m, 2)
            : (decimal?)null;
        equityLines.Add(new StatementLineDto(
            AccountId: null, AccountCode: "", AccountName: "",
            retainedEarnings, compareRetainedEarnings, retainedEarningsVariance, retainedEarningsVariancePercent));

        var totalAssets = assetLines.Sum(l => l.Amount);
        var totalLiabilities = liabilityLines.Sum(l => l.Amount);
        var totalEquity = equityLines.Sum(l => l.Amount);
        decimal? compareTotalAssets = compare is null ? null : assetLines.Sum(l => l.CompareAmount ?? 0);
        decimal? compareTotalLiabilities = compare is null ? null : liabilityLines.Sum(l => l.CompareAmount ?? 0);
        decimal? compareTotalEquity = compare is null ? null : equityLines.Sum(l => l.CompareAmount ?? 0);

        return new BalanceSheetDto(
            asOfDate, compareAsOfDate,
            assetLines, totalAssets, compareTotalAssets,
            liabilityLines, totalLiabilities, compareTotalLiabilities,
            equityLines, totalEquity, compareTotalEquity,
            TotalLiabilitiesAndEquity: totalLiabilities + totalEquity,
            CompareTotalLiabilitiesAndEquity: compare is null ? null : compareTotalLiabilities + compareTotalEquity);
    }
}
