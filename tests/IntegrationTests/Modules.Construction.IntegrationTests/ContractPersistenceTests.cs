using Microsoft.EntityFrameworkCore;
using Modules.Construction.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Construction.IntegrationTests;

public class ContractPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_contract_with_boq_lines_reads_back_identically()
    {
        var wbsElementId = Guid.NewGuid();
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", "30 days net", 10m, 12);
            contract.AddBoqLine("BOQ-001", "Excavation", "حفر", "M3", 100m, 50m, wbsElementId);
            contract.AssignNumber("CON-CONTR-2026-000001");
            writeContext.Contracts.Add(contract);
            await writeContext.SaveChangesAsync();

            contract.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            id = contract.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Contracts.Include(c => c.BoqLines).FirstOrDefaultAsync(c => c.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("CON-CONTR-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Submitted, reloaded.Status);
        Assert.Equal("30 days net", reloaded.PaymentTerms);
        Assert.Equal(10m, reloaded.AdvancePaymentPercentage);
        Assert.Single(reloaded.BoqLines);
        var reloadedLine = reloaded.BoqLines.Single();
        Assert.Equal("حفر", reloadedLine.DescriptionArabic);
        Assert.Equal(wbsElementId, reloadedLine.WbsElementId);
        Assert.Equal(5000m, reloadedLine.Amount);
        Assert.Equal(5000m, reloaded.ContractValue);
    }

    [Fact]
    public async Task Deleting_a_contract_cascades_to_its_boq_lines()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, null, null);
            contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, Guid.NewGuid());
            contract.AssignNumber("CON-CONTR-2026-000002");
            writeContext.Contracts.Add(contract);
            await writeContext.SaveChangesAsync();
            id = contract.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var contract = await deleteContext.Contracts.FirstAsync(c => c.Id == id);
            deleteContext.Contracts.Remove(contract);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remaining = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM construction.boq_lines").SingleAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task RowVersion_increments_across_real_transitions()
    {
        await using var context = TestDatabase.CreateContext();
        var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, null, null);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, Guid.NewGuid());
        contract.AssignNumber("CON-CONTR-2026-000003");
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        var afterCreate = contract.RowVersion;

        contract.Submit("ahmer.bilal");
        await context.SaveChangesAsync();
        var afterSubmit = contract.RowVersion;

        contract.Approve("con.manager");
        await context.SaveChangesAsync();
        var afterApprove = contract.RowVersion;

        Assert.NotEqual(afterCreate, afterSubmit);
        Assert.NotEqual(afterSubmit, afterApprove);
    }
}
