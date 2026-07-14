using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Domain;
using Modules.MasterData.Infrastructure;
using Platform.Core;
using Xunit;

namespace Modules.MasterData.IntegrationTests;

public class ItemPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_item_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var item = new Item("ahmer.bilal", "MAT-1010", "Portland Cement 42.5N", ItemType.Stock, "TON");
            item.UpdateItemNameArabic("أسمنت بورتلاندي");
            item.AssignNumber("MD-ITM-2026-000001");
            writeContext.Items.Add(item);
            await writeContext.SaveChangesAsync();
            id = item.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Items.FirstOrDefaultAsync(i => i.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("MAT-1010", reloaded!.ItemCode);
        Assert.Equal("Portland Cement 42.5N", reloaded.ItemName);
        Assert.Equal("أسمنت بورتلاندي", reloaded.ItemNameArabic);
        Assert.Equal(ItemType.Stock, reloaded.ItemType);
        Assert.Equal("TON", reloaded.UnitOfMeasure);
        Assert.True(reloaded.IsActive);
        Assert.Equal("MD-ITM-2026-000001", reloaded.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Draft, reloaded.Status);
    }

    [Fact]
    public async Task Submit_and_approve_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var item = new Item("ahmer.bilal", "SVC-2001", "Formwork Subcontract Labor", ItemType.Service, "HR");
            item.AssignNumber("MD-ITM-2026-000002");
            writeContext.Items.Add(item);
            await writeContext.SaveChangesAsync();
            item.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            item.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            id = item.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Items.FirstOrDefaultAsync(i => i.Id == id);
        Assert.NotNull(reloaded);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded!.Status);
        Assert.Equal(2, reloaded.RowVersion);
    }

    [Fact]
    public async Task Item_code_uniqueness_is_enforced_at_the_database_level()
    {
        await using var context = TestDatabase.CreateContext();
        var first = new Item("ahmer.bilal", "MAT-9999", "First", ItemType.NonStock, "EA");
        context.Items.Add(first);
        await context.SaveChangesAsync();

        var second = new Item("ahmer.bilal", "MAT-9999", "Second", ItemType.NonStock, "EA");
        context.Items.Add(second);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
