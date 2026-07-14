using Modules.MasterData.Domain;
using Platform.Core;

namespace Modules.MasterData.Tests;

public class GLAccountTests
{
    [Fact]
    public void A_new_account_starts_in_draft_with_no_document_number()
    {
        var account = new GLAccount("ahmer.bilal", "1010", "Cash on Hand", AccountType.Asset);

        Assert.Equal(BusinessObjectStatus.Draft, account.Status);
        Assert.Null(account.DocumentNumber);
        Assert.Equal("1010", account.AccountCode);
        Assert.Equal("Cash on Hand", account.AccountName);
        Assert.Equal(AccountType.Asset, account.AccountType);
        Assert.True(account.IsPostable);
        Assert.True(account.IsActive);
    }

    [Theory]
    [InlineData(AccountType.Asset, "Debit")]
    [InlineData(AccountType.Expense, "Debit")]
    [InlineData(AccountType.Liability, "Credit")]
    [InlineData(AccountType.Equity, "Credit")]
    [InlineData(AccountType.Revenue, "Credit")]
    public void NormalBalance_is_derived_from_account_type(AccountType type, string expected)
    {
        var account = new GLAccount("ahmer.bilal", "9999", "Test", type);
        Assert.Equal(expected, account.NormalBalance);
    }

    [Fact]
    public void Blank_account_code_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new GLAccount("ahmer.bilal", "  ", "Name", AccountType.Asset));
    }

    [Fact]
    public void Blank_account_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new GLAccount("ahmer.bilal", "1010", "", AccountType.Asset));
    }

    [Fact]
    public void Submit_then_approve_reaches_approved_the_same_way_every_BO_does()
    {
        var account = new GLAccount("ahmer.bilal", "1010", "Cash", AccountType.Asset);
        account.AssignNumber("MD-GL-2026-000001");
        account.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, account.Status);
        account.Approve("finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, account.Status);
    }

    [Fact]
    public void Deactivate_marks_inactive_and_can_be_reactivated()
    {
        var account = new GLAccount("ahmer.bilal", "1010", "Cash", AccountType.Asset);
        Assert.True(account.IsActive);
        account.Deactivate();
        Assert.False(account.IsActive);
        account.Activate();
        Assert.True(account.IsActive);
    }

    [Fact]
    public void SetPostable_toggles_header_vs_leaf()
    {
        var account = new GLAccount("ahmer.bilal", "1000", "Current Assets", AccountType.Asset);
        account.SetPostable(false);
        Assert.False(account.IsPostable);
    }
}
