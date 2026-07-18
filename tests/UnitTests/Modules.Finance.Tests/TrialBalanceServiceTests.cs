using Modules.Finance.Application;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;

namespace Modules.Finance.Tests;

public class TrialBalanceServiceTests
{
    private static readonly Guid CurrentAssetsHeaderId = Guid.NewGuid(); // header — no lines ever post here
    private static readonly Guid CashAccountId = Guid.NewGuid();        // leaf, child of CurrentAssetsHeaderId
    private static readonly Guid BankAccountId = Guid.NewGuid();        // leaf, child of CurrentAssetsHeaderId
    private static readonly Guid RevenueAccountId = Guid.NewGuid();     // leaf, top-level (no parent)

    private static (FakeJournalEntryRepository repo, FakeGLAccountLookup accounts) BuildFixture()
    {
        var accounts = new FakeGLAccountLookup();
        accounts.Add(new GLAccountSummary(CurrentAssetsHeaderId, "1000", "Current Assets", "Debit", IsPostable: false, IsActive: true, AccountType: "Asset", ParentAccountId: null));
        accounts.Add(new GLAccountSummary(CashAccountId, "1010", "Cash", "Debit", IsPostable: true, IsActive: true, AccountType: "Asset", ParentAccountId: CurrentAssetsHeaderId));
        accounts.Add(new GLAccountSummary(BankAccountId, "1020", "Bank", "Debit", IsPostable: true, IsActive: true, AccountType: "Asset", ParentAccountId: CurrentAssetsHeaderId));
        accounts.Add(new GLAccountSummary(RevenueAccountId, "4000", "Revenue", "Credit", IsPostable: true, IsActive: true, AccountType: "Revenue", ParentAccountId: null));

        var repo = new FakeJournalEntryRepository();

        // Before the reporting period — becomes Opening balance.
        var opening = new JournalEntry("tester", new DateOnly(2026, 1, 15), "Cash sale, January");
        opening.AddLine(CashAccountId, null, debitAmount: 1000m, creditAmount: 0m);
        opening.AddLine(RevenueAccountId, null, debitAmount: 0m, creditAmount: 1000m);
        opening.Submit("tester"); opening.Approve("tester"); opening.Post("tester");
        repo.Add(opening);

        // Within the reporting period — becomes Period Activity.
        var period = new JournalEntry("tester", new DateOnly(2026, 2, 10), "Bank transfer sale, February");
        period.AddLine(BankAccountId, null, debitAmount: 500m, creditAmount: 0m);
        period.AddLine(RevenueAccountId, null, debitAmount: 0m, creditAmount: 500m);
        period.Submit("tester"); period.Approve("tester"); period.Post("tester");
        repo.Add(period);

        // A Draft entry dated inside the period — must NOT contribute, only Posted entries count.
        var draft = new JournalEntry("tester", new DateOnly(2026, 2, 15), "Unposted draft");
        draft.AddLine(CashAccountId, null, debitAmount: 999m, creditAmount: 0m);
        draft.AddLine(RevenueAccountId, null, debitAmount: 0m, creditAmount: 999m);
        repo.Add(draft);

        return (repo, accounts);
    }

    [Fact]
    public async Task GetAsync_SplitsOpeningAndPeriodActivityByPostingDate()
    {
        var (repo, accounts) = BuildFixture();
        var service = new TrialBalanceService(repo, accounts);

        var result = await service.GetAsync(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        var cash = result.Accounts.Single(a => a.AccountId == CashAccountId);
        Assert.Equal(1000m, cash.OpeningDebit);
        Assert.Equal(0m, cash.PeriodDebit);
        Assert.Equal(1000m, cash.EndingDebit);

        var bank = result.Accounts.Single(a => a.AccountId == BankAccountId);
        Assert.Equal(0m, bank.OpeningDebit);
        Assert.Equal(500m, bank.PeriodDebit);
        Assert.Equal(500m, bank.EndingDebit);
    }

    [Fact]
    public async Task GetAsync_RollsLeafBalancesUpIntoHeaderAccount()
    {
        var (repo, accounts) = BuildFixture();
        var service = new TrialBalanceService(repo, accounts);

        var result = await service.GetAsync(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        var header = result.Accounts.Single(a => a.AccountId == CurrentAssetsHeaderId);
        Assert.True(header.IsHeader);
        Assert.Equal(0, header.Level);
        Assert.Equal(1000m, header.OpeningDebit);  // rolled up from Cash only
        Assert.Equal(500m, header.PeriodDebit);    // rolled up from Bank only
        Assert.Equal(1500m, header.EndingDebit);

        var cash = result.Accounts.Single(a => a.AccountId == CashAccountId);
        var bank = result.Accounts.Single(a => a.AccountId == BankAccountId);
        Assert.False(cash.IsHeader);
        Assert.Equal(1, cash.Level);
        Assert.Equal(1, bank.Level);
    }

    [Fact]
    public async Task GetAsync_TotalsAreBalancedAndExcludeUnpostedEntries()
    {
        var (repo, accounts) = BuildFixture();
        var service = new TrialBalanceService(repo, accounts);

        var result = await service.GetAsync(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        // 1000 (Jan, Cash/Revenue) + 500 (Feb, Bank/Revenue) = 1500 each side; the 999 Draft entry must not count.
        Assert.Equal(1500m, result.TotalEndingDebit);
        Assert.Equal(1500m, result.TotalEndingCredit);
    }
}
