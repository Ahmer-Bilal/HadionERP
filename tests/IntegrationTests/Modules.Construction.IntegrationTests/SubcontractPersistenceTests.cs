using Microsoft.EntityFrameworkCore;
using Modules.Construction.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Construction.IntegrationTests;

public class SubcontractPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_subcontract_with_lines_and_back_charges_reads_back_identically()
    {
        var wbsElementId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var subcontractorId = Guid.NewGuid();
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var subcontract = new Subcontract("ahmer.bilal", projectId, null, subcontractorId, 10m, 15m, 12);
            subcontract.AddLine("SUB-001", "Formwork", "قوالب", "M2", 100m, 50m, wbsElementId);
            subcontract.AssignNumber("CON-SUBCON-2026-000001");
            writeContext.Subcontracts.Add(subcontract);
            await writeContext.SaveChangesAsync();

            subcontract.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            subcontract.Approve("con.manager");
            subcontract.AddBackCharge("Rework of defective formwork", 500m, new DateOnly(2026, 7, 16));
            await writeContext.SaveChangesAsync();
            id = subcontract.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Subcontracts
            .Include(s => s.Lines).Include(s => s.BackCharges)
            .FirstOrDefaultAsync(s => s.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("CON-SUBCON-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal(10m, reloaded.RetentionPercentage);
        Assert.Equal(15m, reloaded.MobilizationAdvancePercentage);
        Assert.Single(reloaded.Lines);
        var reloadedLine = reloaded.Lines.Single();
        Assert.Equal("قوالب", reloadedLine.DescriptionArabic);
        Assert.Equal(wbsElementId, reloadedLine.WbsElementId);
        Assert.Equal(5000m, reloadedLine.Amount);
        Assert.Equal(5000m, reloaded.SubcontractValue);
        Assert.Single(reloaded.BackCharges);
        var reloadedBackCharge = reloaded.BackCharges.Single();
        Assert.Equal(500m, reloadedBackCharge.Amount);
        Assert.Equal(new DateOnly(2026, 7, 16), reloadedBackCharge.DateIncurred);
        Assert.Equal(500m, reloaded.TotalBackCharges);
        Assert.Equal(4500m, reloaded.NetPayableValue);
    }

    [Fact]
    public async Task Deleting_a_subcontract_cascades_to_its_lines_and_back_charges()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var subcontract = new Subcontract("ahmer.bilal", Guid.NewGuid(), null, Guid.NewGuid(), null, null, null);
            subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, Guid.NewGuid());
            subcontract.AssignNumber("CON-SUBCON-2026-000002");
            writeContext.Subcontracts.Add(subcontract);
            await writeContext.SaveChangesAsync();
            subcontract.Submit("ahmer.bilal");
            subcontract.Approve("con.manager");
            subcontract.AddBackCharge("Damage recovery", 100m, new DateOnly(2026, 7, 16));
            await writeContext.SaveChangesAsync();
            id = subcontract.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var subcontract = await deleteContext.Subcontracts.FirstAsync(s => s.Id == id);
            deleteContext.Subcontracts.Remove(subcontract);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLines = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM construction.subcontract_lines").SingleAsync();
        var remainingBackCharges = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM construction.back_charges").SingleAsync();
        Assert.Equal(0, remainingLines);
        Assert.Equal(0, remainingBackCharges);
    }

    [Fact]
    public async Task RowVersion_increments_across_real_transitions()
    {
        await using var context = TestDatabase.CreateContext();
        var subcontract = new Subcontract("ahmer.bilal", Guid.NewGuid(), null, Guid.NewGuid(), null, null, null);
        subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, Guid.NewGuid());
        subcontract.AssignNumber("CON-SUBCON-2026-000003");
        context.Subcontracts.Add(subcontract);
        await context.SaveChangesAsync();
        var afterCreate = subcontract.RowVersion;

        subcontract.Submit("ahmer.bilal");
        await context.SaveChangesAsync();
        var afterSubmit = subcontract.RowVersion;

        subcontract.Approve("con.manager");
        await context.SaveChangesAsync();
        var afterApprove = subcontract.RowVersion;

        Assert.NotEqual(afterCreate, afterSubmit);
        Assert.NotEqual(afterSubmit, afterApprove);
    }
}
