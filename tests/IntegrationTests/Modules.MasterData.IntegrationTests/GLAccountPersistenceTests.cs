using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Domain;
using Modules.MasterData.Infrastructure;
using Platform.Core;
using Xunit;

namespace Modules.MasterData.IntegrationTests;

public class GLAccountPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_account_reads_back_identically_through_a_fresh_DbContext()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var parent = new GLAccount("ahmer.bilal", "1000", "Current Assets", AccountType.Asset);
            parent.SetPostable(false);
            parent.UpdateAccountNameArabic("الأصول المتداولة");
            parent.AssignNumber("MD-GL-2026-000001");
            writeContext.GLAccounts.Add(parent);
            await writeContext.SaveChangesAsync();
            parentId = parent.Id;

            var child = new GLAccount("ahmer.bilal", "1010", "Cash on Hand", AccountType.Asset);
            child.UpdateAccountNameArabic("النقدية في الصندوق");
            child.AssignParent(parentId);
            child.AssignNumber("MD-GL-2026-000002");
            writeContext.GLAccounts.Add(child);
            await writeContext.SaveChangesAsync();
            childId = child.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloadedParent = await readContext.GLAccounts.FirstOrDefaultAsync(a => a.Id == parentId);
        var reloadedChild = await readContext.GLAccounts.FirstOrDefaultAsync(a => a.Id == childId);

        Assert.NotNull(reloadedParent);
        Assert.Equal("1000", reloadedParent!.AccountCode);
        Assert.Equal("Current Assets", reloadedParent.AccountName);
        Assert.Equal("الأصول المتداولة", reloadedParent.AccountNameArabic);
        Assert.Equal("Asset", reloadedParent.AccountType.ToString());
        Assert.False(reloadedParent.IsPostable);
        Assert.True(reloadedParent.IsActive);
        Assert.Null(reloadedParent.ParentAccountId);

        Assert.NotNull(reloadedChild);
        Assert.Equal(parentId, reloadedChild!.ParentAccountId);
        Assert.Equal("النقدية في الصندوق", reloadedChild.AccountNameArabic);
    }

    [Fact]
    public async Task Submit_and_approve_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var account = new GLAccount("ahmer.bilal", "2000", "Accounts Payable", AccountType.Liability);
            account.AssignNumber("MD-GL-2026-000003");
            writeContext.GLAccounts.Add(account);
            await writeContext.SaveChangesAsync();
            account.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            account.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            id = account.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.GLAccounts.FirstOrDefaultAsync(a => a.Id == id);
        Assert.NotNull(reloaded);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded!.Status);
        Assert.Equal(2, reloaded.RowVersion);
    }
}
