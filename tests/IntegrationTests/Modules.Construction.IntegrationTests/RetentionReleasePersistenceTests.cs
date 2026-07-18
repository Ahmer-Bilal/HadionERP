using Microsoft.EntityFrameworkCore;
using Modules.Construction.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Construction.IntegrationTests;

public class RetentionReleasePersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateOnly ReleaseDate = new(2026, 8, 1);

    [Fact]
    public async Task A_saved_Contract_release_with_AR_billing_accounts_reads_back_identically()
    {
        var projectId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var revenueAccountId = Guid.NewGuid();
        var receivableAccountId = Guid.NewGuid();
        var arInvoiceId = Guid.NewGuid();
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var release = new RetentionRelease(
                "ahmer.bilal", projectId, CommercialDocumentType.Contract, contractId, ReleaseDate, 150m,
                RetentionTriggerEvent.TakingOver, revenueAccountId, receivableAccountId);
            release.AssignNumber("CON-RETREL-2026-000001");
            writeContext.RetentionReleases.Add(release);
            await writeContext.SaveChangesAsync();

            release.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            release.LinkArInvoice(arInvoiceId);
            release.Approve("commercial.manager");
            await writeContext.SaveChangesAsync();
            id = release.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.RetentionReleases.FirstOrDefaultAsync(r => r.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("CON-RETREL-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal(CommercialDocumentType.Contract, reloaded.CommercialDocumentType);
        Assert.Equal(contractId, reloaded.CommercialDocumentId);
        Assert.Equal(150m, reloaded.AmountReleased);
        Assert.Equal(RetentionTriggerEvent.TakingOver, reloaded.TriggerEvent);
        Assert.Equal(revenueAccountId, reloaded.RevenueAccountId);
        Assert.Equal(receivableAccountId, reloaded.ReceivableAccountId);
        Assert.Equal(arInvoiceId, reloaded.LinkedArInvoiceId);
        Assert.Null(reloaded.ExpenseAccountId);
        Assert.Null(reloaded.PayableAccountId);
        Assert.Null(reloaded.LinkedApInvoiceId);
    }

    [Fact]
    public async Task A_saved_Subcontract_release_with_AP_billing_accounts_reads_back_identically()
    {
        var expenseAccountId = Guid.NewGuid();
        var payableAccountId = Guid.NewGuid();
        var apInvoiceId = Guid.NewGuid();
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var release = new RetentionRelease(
                "ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Subcontract, Guid.NewGuid(), ReleaseDate, 80m,
                RetentionTriggerEvent.DefectsLiabilityExpiry,
                expenseAccountId: expenseAccountId, payableAccountId: payableAccountId);
            release.AssignNumber("CON-RETREL-2026-000002");
            writeContext.RetentionReleases.Add(release);
            await writeContext.SaveChangesAsync();

            release.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            release.LinkApInvoice(apInvoiceId);
            release.Approve("commercial.manager");
            await writeContext.SaveChangesAsync();
            id = release.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.RetentionReleases.FirstOrDefaultAsync(r => r.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal(CommercialDocumentType.Subcontract, reloaded!.CommercialDocumentType);
        Assert.Equal(RetentionTriggerEvent.DefectsLiabilityExpiry, reloaded.TriggerEvent);
        Assert.Equal(expenseAccountId, reloaded.ExpenseAccountId);
        Assert.Equal(payableAccountId, reloaded.PayableAccountId);
        Assert.Equal(apInvoiceId, reloaded.LinkedApInvoiceId);
        Assert.Null(reloaded.RevenueAccountId);
        Assert.Null(reloaded.ReceivableAccountId);
        Assert.Null(reloaded.LinkedArInvoiceId);
    }

    [Fact]
    public async Task RowVersion_increments_across_real_transitions()
    {
        await using var context = TestDatabase.CreateContext();
        var release = new RetentionRelease(
            "ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Contract, Guid.NewGuid(), ReleaseDate, 50m, RetentionTriggerEvent.Manual);
        release.AssignNumber("CON-RETREL-2026-000003");
        context.RetentionReleases.Add(release);
        await context.SaveChangesAsync();
        var afterCreate = release.RowVersion;

        release.Submit("ahmer.bilal");
        await context.SaveChangesAsync();
        var afterSubmit = release.RowVersion;

        release.Approve("commercial.manager");
        await context.SaveChangesAsync();
        var afterApprove = release.RowVersion;

        Assert.NotEqual(afterCreate, afterSubmit);
        Assert.NotEqual(afterSubmit, afterApprove);
    }

    [Fact]
    public async Task A_saved_Contracts_own_RetentionPercentage_reads_back_identically()
    {
        var projectId = Guid.NewGuid();
        var wbsElementId = Guid.NewGuid();
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var contract = new Contract("ahmer.bilal", projectId, "LumpSum", null, 15m, null, retentionPercentage: 10m);
            contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, wbsElementId);
            contract.AssignNumber("CON-CONTR-2026-000099");
            writeContext.Contracts.Add(contract);
            await writeContext.SaveChangesAsync();
            id = contract.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Contracts.FirstOrDefaultAsync(c => c.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal(10m, reloaded!.RetentionPercentage);
    }
}
