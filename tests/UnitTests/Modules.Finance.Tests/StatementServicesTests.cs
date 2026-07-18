using Modules.Finance.Application;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;

namespace Modules.Finance.Tests;

public class StatementServicesTests
{
    private static readonly Guid CashAccountId = Guid.NewGuid();     // Asset
    private static readonly Guid RevenueAccountId = Guid.NewGuid();  // Revenue
    private static readonly Guid ExpenseAccountId = Guid.NewGuid();  // Expense
    private static readonly Guid PayableAccountId = Guid.NewGuid();  // Liability
    private static readonly Guid CapitalAccountId = Guid.NewGuid();  // Equity

    private static (TrialBalanceService trialBalance, IncomeStatementService incomeStatement, BalanceSheetService balanceSheet) BuildFixture()
    {
        var accounts = new FakeGLAccountLookup();
        accounts.Add(new GLAccountSummary(CashAccountId, "1010", "Cash", "Debit", IsPostable: true, IsActive: true, AccountType: "Asset", ParentAccountId: null));
        accounts.Add(new GLAccountSummary(RevenueAccountId, "4000", "Sales Revenue", "Credit", IsPostable: true, IsActive: true, AccountType: "Revenue", ParentAccountId: null));
        accounts.Add(new GLAccountSummary(ExpenseAccountId, "5000", "Operating Expense", "Debit", IsPostable: true, IsActive: true, AccountType: "Expense", ParentAccountId: null));
        accounts.Add(new GLAccountSummary(PayableAccountId, "2010", "Accounts Payable", "Credit", IsPostable: true, IsActive: true, AccountType: "Liability", ParentAccountId: null));
        accounts.Add(new GLAccountSummary(CapitalAccountId, "3000", "Owner's Capital", "Credit", IsPostable: true, IsActive: true, AccountType: "Equity", ParentAccountId: null));

        var repo = new FakeJournalEntryRepository();

        void Post(DateOnly postingDate, string description, Guid debitAccount, decimal debitAmount, Guid creditAccount, decimal creditAmount)
        {
            var entry = new JournalEntry("tester", postingDate, description);
            entry.AddLine(debitAccount, null, debitAmount, 0m);
            entry.AddLine(creditAccount, null, 0m, creditAmount);
            entry.Submit("tester"); entry.Approve("tester"); entry.Post("tester");
            repo.Add(entry);
        }

        // January (comparison period)
        Post(new DateOnly(2026, 1, 10), "January sale", CashAccountId, 1000m, RevenueAccountId, 1000m);
        Post(new DateOnly(2026, 1, 15), "January expense paid in cash", ExpenseAccountId, 200m, CashAccountId, 200m);
        Post(new DateOnly(2026, 1, 20), "Owner capital contribution", CashAccountId, 2000m, CapitalAccountId, 2000m);

        // February (current period)
        Post(new DateOnly(2026, 2, 5), "February sale", CashAccountId, 500m, RevenueAccountId, 500m);
        Post(new DateOnly(2026, 2, 10), "February expense on credit", ExpenseAccountId, 100m, PayableAccountId, 100m);

        var trialBalance = new TrialBalanceService(repo, accounts);
        return (trialBalance, new IncomeStatementService(trialBalance), new BalanceSheetService(trialBalance));
    }

    [Fact]
    public async Task IncomeStatement_ComputesCurrentAndComparePeriodWithVariance()
    {
        var (_, incomeStatement, _) = BuildFixture();

        var result = await incomeStatement.GetAsync(
            new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(500m, result.TotalRevenue);
        Assert.Equal(1000m, result.CompareTotalRevenue);
        Assert.Equal(100m, result.TotalExpenses);
        Assert.Equal(200m, result.CompareTotalExpenses);
        Assert.Equal(400m, result.NetProfit);
        Assert.Equal(800m, result.CompareNetProfit);

        var revenueLine = Assert.Single(result.RevenueLines);
        Assert.Equal(500m, revenueLine.Amount);
        Assert.Equal(1000m, revenueLine.CompareAmount);
        Assert.Equal(-500m, revenueLine.Variance);
        Assert.Equal(-50m, revenueLine.VariancePercent);
    }

    [Fact]
    public async Task IncomeStatement_WithoutComparePeriod_LeavesCompareFieldsNull()
    {
        var (_, incomeStatement, _) = BuildFixture();

        var result = await incomeStatement.GetAsync(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28), null, null);

        Assert.Null(result.CompareTotalRevenue);
        Assert.Null(result.CompareNetProfit);
        Assert.All(result.RevenueLines, l => Assert.Null(l.CompareAmount));
    }

    [Fact]
    public async Task BalanceSheet_AssetsEqualLiabilitiesPlusEquityIncludingRetainedEarnings()
    {
        var (_, _, balanceSheet) = BuildFixture();

        var result = await balanceSheet.GetAsync(new DateOnly(2026, 2, 28), new DateOnly(2026, 1, 31));

        // Current (as of 28-Feb): Cash 3300 Dr; Payable 100 Cr; Capital 2000 Cr; Retained Earnings 1200 Cr.
        Assert.Equal(3300m, result.TotalAssets);
        Assert.Equal(100m, result.TotalLiabilities);
        Assert.Equal(3200m, result.TotalEquity); // 2000 Capital + 1200 Retained Earnings
        Assert.Equal(result.TotalAssets, result.TotalLiabilitiesAndEquity);

        var retainedEarnings = Assert.Single(result.EquityLines, l => l.AccountId is null);
        Assert.Equal(1200m, retainedEarnings.Amount);

        // Compare (as of 31-Jan): Cash 2800 Dr; Payable 0; Capital 2000 Cr; Retained Earnings 800 Cr.
        Assert.Equal(2800m, result.CompareTotalAssets);
        Assert.Equal(0m, result.CompareTotalLiabilities);
        Assert.Equal(2800m, result.CompareTotalEquity); // 2000 Capital + 800 Retained Earnings
        Assert.Equal(result.CompareTotalAssets, result.CompareTotalLiabilitiesAndEquity);
    }

    [Fact]
    public async Task BalanceSheet_WithoutCompareDate_LeavesCompareFieldsNull()
    {
        var (_, _, balanceSheet) = BuildFixture();

        var result = await balanceSheet.GetAsync(new DateOnly(2026, 2, 28), null);

        Assert.Null(result.CompareTotalAssets);
        Assert.Null(result.CompareTotalLiabilitiesAndEquity);
    }
}
