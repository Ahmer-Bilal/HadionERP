using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Domain;
using Modules.MasterData.Infrastructure;
using Platform.Core;
using Xunit;

namespace Modules.MasterData.IntegrationTests;

public class CostCenterPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_cost_center_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid parentId;
        Guid childId;

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var parent = new CostCenter("ahmer.bilal", "CC-1000", "Head Office");
            parent.SetPostable(false);
            parent.UpdateCostCenterNameArabic("المكتب الرئيسي");
            parent.AssignNumber("MD-CC-2026-000001");
            writeContext.CostCenters.Add(parent);
            await writeContext.SaveChangesAsync();
            parentId = parent.Id;

            var child = new CostCenter("ahmer.bilal", "CC-1010", "Finance Department");
            child.UpdateCostCenterNameArabic("إدارة المالية");
            child.AssignParent(parentId);
            child.AssignNumber("MD-CC-2026-000002");
            writeContext.CostCenters.Add(child);
            await writeContext.SaveChangesAsync();
            childId = child.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloadedParent = await readContext.CostCenters.FirstOrDefaultAsync(c => c.Id == parentId);
        var reloadedChild = await readContext.CostCenters.FirstOrDefaultAsync(c => c.Id == childId);

        Assert.NotNull(reloadedParent);
        Assert.Equal("CC-1000", reloadedParent!.CostCenterCode);
        Assert.Equal("Head Office", reloadedParent.CostCenterName);
        Assert.Equal("المكتب الرئيسي", reloadedParent.CostCenterNameArabic);
        Assert.False(reloadedParent.IsPostable);
        Assert.True(reloadedParent.IsActive);
        Assert.Null(reloadedParent.ParentCostCenterId);

        Assert.NotNull(reloadedChild);
        Assert.Equal(parentId, reloadedChild!.ParentCostCenterId);
        Assert.Equal("إدارة المالية", reloadedChild.CostCenterNameArabic);
    }

    [Fact]
    public async Task Submit_and_approve_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var costCenter = new CostCenter("ahmer.bilal", "CC-2000", "Operations");
            costCenter.AssignNumber("MD-CC-2026-000003");
            writeContext.CostCenters.Add(costCenter);
            await writeContext.SaveChangesAsync();
            costCenter.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            costCenter.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            id = costCenter.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.CostCenters.FirstOrDefaultAsync(c => c.Id == id);
        Assert.NotNull(reloaded);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded!.Status);
        Assert.Equal(2, reloaded.RowVersion);
    }

    [Fact]
    public async Task Cost_center_code_uniqueness_is_enforced_at_the_database_level()
    {
        await using var context = TestDatabase.CreateContext();
        var first = new CostCenter("ahmer.bilal", "CC-9999", "First");
        context.CostCenters.Add(first);
        await context.SaveChangesAsync();

        var second = new CostCenter("ahmer.bilal", "CC-9999", "Second");
        context.CostCenters.Add(second);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
