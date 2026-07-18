namespace Modules.Finance.Application;

/// <summary>
/// The Income Statement (Profit &amp; Loss) report — like <see cref="TrialBalanceService"/>, not a new
/// Business Object, just a different presentation of the same posted-ledger data. Reuses
/// <see cref="TrialBalanceService"/> directly (an intra-module dependency — both live in
/// Modules.Finance.Application, so no Contracts package is needed, the same "APInvoiceService reuses
/// JournalEntryService directly" precedent) rather than re-querying <c>IJournalEntryRepository</c>/
/// <c>IGLAccountLookup</c> itself: Trial Balance already computes each account's Period Debit/Credit
/// movement, and Income Statement is exactly "the Revenue and Expense accounts' movement for the period,
/// netted to their normal balance side and totaled."
/// </summary>
public sealed class IncomeStatementService
{
    private readonly TrialBalanceService _trialBalanceService;

    public IncomeStatementService(TrialBalanceService trialBalanceService) => _trialBalanceService = trialBalanceService;

    public async Task<IncomeStatementDto> GetAsync(
        DateOnly periodStart, DateOnly periodEnd,
        DateOnly? comparePeriodStart, DateOnly? comparePeriodEnd,
        CancellationToken cancellationToken = default)
    {
        var current = await _trialBalanceService.GetAsync(periodStart, periodEnd, cancellationToken);
        var compare = comparePeriodStart is { } compareStart && comparePeriodEnd is { } compareEnd
            ? await _trialBalanceService.GetAsync(compareStart, compareEnd, cancellationToken)
            : null;
        var compareByAccount = compare?.Accounts.ToDictionary(a => a.AccountId)
            ?? new Dictionary<Guid, TrialBalanceAccountDto>();

        // Revenue is credit-normal (net = Credit - Debit); Expense is debit-normal (net = Debit - Credit) —
        // the same GLAccount.NormalBalance convention Trial Balance itself just reports raw, applied here to
        // collapse each account's two raw columns into the single signed "how much P&L effect this period"
        // figure a statement (as opposed to a ledger) actually shows.
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

        var revenueLines = BuildLines("Revenue", a => a.PeriodCredit - a.PeriodDebit);
        var expenseLines = BuildLines("Expense", a => a.PeriodDebit - a.PeriodCredit);

        var totalRevenue = revenueLines.Sum(l => l.Amount);
        var totalExpenses = expenseLines.Sum(l => l.Amount);
        decimal? compareTotalRevenue = compare is null ? null : revenueLines.Sum(l => l.CompareAmount ?? 0);
        decimal? compareTotalExpenses = compare is null ? null : expenseLines.Sum(l => l.CompareAmount ?? 0);

        return new IncomeStatementDto(
            periodStart, periodEnd, comparePeriodStart, comparePeriodEnd,
            revenueLines, totalRevenue, compareTotalRevenue,
            expenseLines, totalExpenses, compareTotalExpenses,
            NetProfit: totalRevenue - totalExpenses,
            CompareNetProfit: compare is null ? null : compareTotalRevenue - compareTotalExpenses);
    }
}
