using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Procurement.IntegrationTests;

public class PurchaseRequisitionPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_requisition_with_its_lines_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        var itemId = Guid.NewGuid();
        var costCenterId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var requisition = new PurchaseRequisition("ahmer.bilal", "Rebar for Tower A", new DateOnly(2026, 8, 1));
            requisition.AddLine(itemId, costCenterId, 10, 500, "12mm rebar");
            requisition.AssignNumber("PROC-PR-2026-000001");
            writeContext.PurchaseRequisitions.Add(requisition);
            await writeContext.SaveChangesAsync();
            id = requisition.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.PurchaseRequisitions.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("PROC-PR-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal("Rebar for Tower A", reloaded.Description);
        Assert.Equal(new DateOnly(2026, 8, 1), reloaded.RequiredByDate);
        Assert.Single(reloaded.Lines);
        Assert.Equal(5000, reloaded.EstimatedTotal);

        var line = reloaded.Lines.Single();
        Assert.Equal(itemId, line.ItemId);
        Assert.Equal(costCenterId, line.CostCenterId);
        Assert.Equal("12mm rebar", line.LineDescription);
    }

    [Fact]
    public async Task Submit_approve_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var requisition = new PurchaseRequisition("ahmer.bilal", "Test");
            requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, 100);
            requisition.AssignNumber("PROC-PR-2026-000002");
            writeContext.PurchaseRequisitions.Add(requisition);
            await writeContext.SaveChangesAsync();
            requisition.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            requisition.Approve("procurement.manager");
            await writeContext.SaveChangesAsync();
            id = requisition.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.PurchaseRequisitions.FirstAsync(r => r.Id == id);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal(2, reloaded.RowVersion); // Submit, Approve = two transitions
    }

    [Fact]
    public async Task Deleting_a_requisition_cascades_to_its_lines()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var requisition = new PurchaseRequisition("ahmer.bilal", "Test");
            requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, 100);
            requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 2, 50);
            requisition.AssignNumber("PROC-PR-2026-000003");
            writeContext.PurchaseRequisitions.Add(requisition);
            await writeContext.SaveChangesAsync();
            id = requisition.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var requisition = await deleteContext.PurchaseRequisitions.FirstAsync(r => r.Id == id);
            deleteContext.PurchaseRequisitions.Remove(requisition);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLineCount = await readContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM procurement.purchase_requisition_lines")
            .SingleAsync();
        Assert.Equal(0, remainingLineCount);
    }
}
