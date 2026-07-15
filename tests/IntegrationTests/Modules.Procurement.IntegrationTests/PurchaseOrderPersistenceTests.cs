using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Procurement.IntegrationTests;

public class PurchaseOrderPersistenceTests : IAsyncLifetime
{
    private static readonly Guid VendorId = Guid.NewGuid();

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_po_with_lines_reads_back_identically()
    {
        Guid id;
        var itemId = Guid.NewGuid();
        var costCenterId = Guid.NewGuid();
        var rfqId = Guid.NewGuid();
        var rfqLineId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var po = new PurchaseOrder("ahmer.bilal", VendorId, rfqId);
            po.AddLine(itemId, costCenterId, 100, 27.5m, rfqLineId);
            po.AssignNumber("PROC-PO-2026-000001");
            writeContext.PurchaseOrders.Add(po);
            await writeContext.SaveChangesAsync();

            po.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            id = po.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("PROC-PO-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Submitted, reloaded.Status);
        Assert.Equal(VendorId, reloaded.VendorId);
        Assert.Equal(rfqId, reloaded.RequestForQuotationId);
        Assert.Single(reloaded.Lines);
        var line = reloaded.Lines.Single();
        Assert.Equal(itemId, line.ItemId);
        Assert.Equal(costCenterId, line.CostCenterId);
        Assert.Equal(27.5m, line.UnitPrice);
        Assert.Equal(rfqLineId, line.RfqLineId);
        Assert.Equal(2750m, reloaded.Total);
    }

    [Fact]
    public async Task A_direct_po_with_no_rfq_persists_a_null_rfq_id()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var po = new PurchaseOrder("ahmer.bilal", VendorId);
            po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 5, 10);
            po.AssignNumber("PROC-PO-2026-000002");
            writeContext.PurchaseOrders.Add(po);
            await writeContext.SaveChangesAsync();
            id = po.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id);
        Assert.NotNull(reloaded);
        Assert.Null(reloaded!.RequestForQuotationId);
    }

    [Fact]
    public async Task Deleting_a_po_cascades_to_its_lines()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var po = new PurchaseOrder("ahmer.bilal", VendorId);
            po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 5, 10);
            po.AssignNumber("PROC-PO-2026-000003");
            writeContext.PurchaseOrders.Add(po);
            await writeContext.SaveChangesAsync();
            id = po.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var po = await deleteContext.PurchaseOrders.FirstAsync(p => p.Id == id);
            deleteContext.PurchaseOrders.Remove(po);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLines = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM procurement.purchase_order_lines").SingleAsync();
        Assert.Equal(0, remainingLines);
    }
}
