using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Procurement.IntegrationTests;

public class GoodsReceiptNotePersistenceTests : IAsyncLifetime
{
    private static readonly Guid PurchaseOrderId = Guid.NewGuid();

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_grn_with_lines_reads_back_identically()
    {
        Guid id;
        var poLineId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var grn = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 20));
            grn.AddLine(poLineId, itemId, 40, 10);
            grn.AssignNumber("PROC-GRN-2026-000001");
            writeContext.GoodsReceiptNotes.Add(grn);
            await writeContext.SaveChangesAsync();

            grn.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            id = grn.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.GoodsReceiptNotes.Include(g => g.Lines).FirstOrDefaultAsync(g => g.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("PROC-GRN-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Submitted, reloaded.Status);
        Assert.Equal(PurchaseOrderId, reloaded.PurchaseOrderId);
        Assert.Single(reloaded.Lines);
        var line = reloaded.Lines.Single();
        Assert.Equal(poLineId, line.PurchaseOrderLineId);
        Assert.Equal(itemId, line.ItemId);
        Assert.Equal(40, line.QuantityReceived);
        Assert.Equal(10, line.UnitPrice);
        Assert.Equal(400, reloaded.ReceivedValue);
    }

    [Fact]
    public async Task Deleting_a_grn_cascades_to_its_lines()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var grn = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 20));
            grn.AddLine(Guid.NewGuid(), Guid.NewGuid(), 5, 10);
            grn.AssignNumber("PROC-GRN-2026-000002");
            writeContext.GoodsReceiptNotes.Add(grn);
            await writeContext.SaveChangesAsync();
            id = grn.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var grn = await deleteContext.GoodsReceiptNotes.FirstAsync(g => g.Id == id);
            deleteContext.GoodsReceiptNotes.Remove(grn);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLines = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM procurement.grn_lines").SingleAsync();
        Assert.Equal(0, remainingLines);
    }

    [Fact]
    public async Task ListByPurchaseOrderAsync_finds_every_grn_against_that_po_regardless_of_status()
    {
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var grn1 = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 20));
            grn1.AddLine(Guid.NewGuid(), Guid.NewGuid(), 5, 10);
            grn1.AssignNumber("PROC-GRN-2026-000003");

            var grn2 = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 21));
            grn2.AddLine(Guid.NewGuid(), Guid.NewGuid(), 3, 10);
            grn2.AssignNumber("PROC-GRN-2026-000004");

            writeContext.GoodsReceiptNotes.AddRange(grn1, grn2);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var forThisPo = await readContext.GoodsReceiptNotes.Include(g => g.Lines)
            .Where(g => g.PurchaseOrderId == PurchaseOrderId).ToListAsync();
        Assert.Equal(2, forThisPo.Count);
    }
}
