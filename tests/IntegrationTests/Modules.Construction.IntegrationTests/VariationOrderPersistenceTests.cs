using Microsoft.EntityFrameworkCore;
using Modules.Construction.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Construction.IntegrationTests;

public class VariationOrderPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_variation_order_with_an_adjustment_and_a_new_line_reads_back_identically()
    {
        var projectId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var contractLineId = Guid.NewGuid();
        var wbsElementId = Guid.NewGuid();
        Guid id;

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var order = new VariationOrder("ahmer.bilal", projectId, CommercialDocumentType.Contract, contractId, "Additional excavation scope");
            order.AddLineAdjustment(contractLineId, 20m, 50m);
            order.AddNewLine("BOQ-002", "New Item", null, "M2", 10m, 30m, wbsElementId);
            order.AssignNumber("CON-VO-2026-000001");
            writeContext.VariationOrders.Add(order);
            await writeContext.SaveChangesAsync();

            order.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            order.Approve("con.manager");
            await writeContext.SaveChangesAsync();
            id = order.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.VariationOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("CON-VO-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal("Additional excavation scope", reloaded.Reason);
        Assert.Equal(2, reloaded.Lines.Count);
        Assert.Equal(1300m, reloaded.TotalValue); // (20*50) + (10*30)

        var adjustmentLine = reloaded.Lines.Single(l => l.CommercialDocumentLineId == contractLineId);
        Assert.Equal(20m, adjustmentLine.QuantityDelta);
        Assert.Equal(50m, adjustmentLine.Rate);

        var newLine = reloaded.Lines.Single(l => l.Code == "BOQ-002");
        Assert.Equal(10m, newLine.QuantityDelta);
        Assert.Equal(30m, newLine.Rate);
        Assert.Equal(wbsElementId, newLine.WbsElementId);
    }

    [Fact]
    public async Task Deleting_a_variation_order_cascades_to_its_lines()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var order = new VariationOrder("ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Subcontract, Guid.NewGuid(), "Scope change");
            order.AddLineAdjustment(Guid.NewGuid(), -5m, 40m);
            order.AssignNumber("CON-VO-2026-000002");
            writeContext.VariationOrders.Add(order);
            await writeContext.SaveChangesAsync();
            id = order.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var order = await deleteContext.VariationOrders.FirstAsync(o => o.Id == id);
            deleteContext.VariationOrders.Remove(order);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLines = await readContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM construction.variation_order_lines").SingleAsync();
        Assert.Equal(0, remainingLines);
    }

    [Fact]
    public async Task RowVersion_increments_across_real_transitions()
    {
        await using var context = TestDatabase.CreateContext();
        var order = new VariationOrder("ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Contract, Guid.NewGuid(), "Scope change");
        order.AddLineAdjustment(Guid.NewGuid(), 5m, 20m);
        order.AssignNumber("CON-VO-2026-000003");
        context.VariationOrders.Add(order);
        await context.SaveChangesAsync();
        var afterCreate = order.RowVersion;

        order.Submit("ahmer.bilal");
        await context.SaveChangesAsync();
        var afterSubmit = order.RowVersion;

        order.Approve("con.manager");
        await context.SaveChangesAsync();
        var afterApprove = order.RowVersion;

        Assert.NotEqual(afterCreate, afterSubmit);
        Assert.NotEqual(afterSubmit, afterApprove);
    }
}
