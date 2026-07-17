using Microsoft.EntityFrameworkCore;
using Modules.Construction.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Construction.IntegrationTests;

public class IpcPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateOnly PeriodStart = new(2026, 7, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 7, 31);

    [Fact]
    public async Task A_saved_ipc_with_lines_reads_back_identically()
    {
        var projectId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var measurementSheetId = Guid.NewGuid();
        var contractLineId = Guid.NewGuid();
        var revenueAccountId = Guid.NewGuid();
        var receivableAccountId = Guid.NewGuid();
        var arInvoiceId = Guid.NewGuid();
        Guid id;
        Guid lineId;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var ipc = new Ipc(
                "ahmer.bilal", projectId, CommercialDocumentType.Contract, contractId, measurementSheetId,
                PeriodStart, PeriodEnd, retentionPercentageApplied: 10m, advancePaymentPercentageApplied: 15m, otherDeductions: 50m,
                revenueAccountId, receivableAccountId);
            var line = ipc.AddLine(contractLineId, 50m, 40m, 100m);
            ipc.AssignNumber("CON-IPC-2026-000001");
            writeContext.Ipcs.Add(ipc);
            await writeContext.SaveChangesAsync();

            ipc.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            ipc.LinkArInvoice(arInvoiceId);
            ipc.Approve("engineer");
            await writeContext.SaveChangesAsync();
            id = ipc.Id;
            lineId = line.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Ipcs.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("CON-IPC-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal(CommercialDocumentType.Contract, reloaded.CommercialDocumentType);
        Assert.Equal(measurementSheetId, reloaded.MeasurementSheetId);
        Assert.Equal(10m, reloaded.RetentionPercentageApplied);
        Assert.Equal(15m, reloaded.AdvancePaymentPercentageApplied);
        Assert.Equal(50m, reloaded.OtherDeductions);
        Assert.Equal(revenueAccountId, reloaded.RevenueAccountId);
        Assert.Equal(receivableAccountId, reloaded.ReceivableAccountId);
        Assert.Equal(arInvoiceId, reloaded.LinkedArInvoiceId);
        var reloadedLine = Assert.Single(reloaded.Lines);
        Assert.Equal(lineId, reloadedLine.Id);
        Assert.Equal(contractLineId, reloadedLine.CommercialDocumentLineId);
        Assert.Equal(50m, reloadedLine.Rate);
        Assert.Equal(40m, reloadedLine.QuantityThisPeriod);
        Assert.Equal(100m, reloadedLine.QuantityToDate);
        Assert.Equal(2000m, reloadedLine.ValueThisPeriod);
        Assert.Equal(5000m, reloadedLine.ValueToDate);
        Assert.Equal(2000m, reloaded.GrossValueThisPeriod);
        Assert.Equal(200m, reloaded.RetentionAmount);
        Assert.Equal(300m, reloaded.AdvanceRecoveryAmount);
        Assert.Equal(1450m, reloaded.NetPayable);
    }

    [Fact]
    public async Task A_saved_Subcontract_ipc_with_AP_billing_accounts_reads_back_identically()
    {
        var expenseAccountId = Guid.NewGuid();
        var payableAccountId = Guid.NewGuid();
        var apInvoiceId = Guid.NewGuid();
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var ipc = new Ipc(
                "ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Subcontract, Guid.NewGuid(), Guid.NewGuid(),
                PeriodStart, PeriodEnd, retentionPercentageApplied: 10m, advancePaymentPercentageApplied: null, otherDeductions: 0m,
                expenseAccountId: expenseAccountId, payableAccountId: payableAccountId);
            ipc.AddLine(Guid.NewGuid(), 40m, 20m, 20m);
            ipc.AssignNumber("CON-IPC-2026-000004");
            writeContext.Ipcs.Add(ipc);
            await writeContext.SaveChangesAsync();

            ipc.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            ipc.LinkApInvoice(apInvoiceId);
            ipc.Approve("engineer");
            await writeContext.SaveChangesAsync();
            id = ipc.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Ipcs.FirstOrDefaultAsync(i => i.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal(CommercialDocumentType.Subcontract, reloaded!.CommercialDocumentType);
        Assert.Equal(expenseAccountId, reloaded.ExpenseAccountId);
        Assert.Equal(payableAccountId, reloaded.PayableAccountId);
        Assert.Equal(apInvoiceId, reloaded.LinkedApInvoiceId);
        Assert.Null(reloaded.RevenueAccountId);
        Assert.Null(reloaded.ReceivableAccountId);
        Assert.Null(reloaded.LinkedArInvoiceId);
    }

    [Fact]
    public async Task Deleting_an_ipc_cascades_to_its_lines()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var ipc = new Ipc(
                "ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Subcontract, Guid.NewGuid(), Guid.NewGuid(),
                PeriodStart, PeriodEnd, null, null, 0m);
            ipc.AddLine(Guid.NewGuid(), 10m, 5m, 5m);
            ipc.AssignNumber("CON-IPC-2026-000002");
            writeContext.Ipcs.Add(ipc);
            await writeContext.SaveChangesAsync();
            id = ipc.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var ipc = await deleteContext.Ipcs.FirstAsync(i => i.Id == id);
            deleteContext.Ipcs.Remove(ipc);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLines = await readContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM construction.ipc_lines").SingleAsync();
        Assert.Equal(0, remainingLines);
    }

    [Fact]
    public async Task RowVersion_increments_across_real_transitions()
    {
        await using var context = TestDatabase.CreateContext();
        var ipc = new Ipc(
            "ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Contract, Guid.NewGuid(), Guid.NewGuid(),
            PeriodStart, PeriodEnd, null, null, 0m);
        ipc.AddLine(Guid.NewGuid(), 10m, 5m, 5m);
        ipc.AssignNumber("CON-IPC-2026-000003");
        context.Ipcs.Add(ipc);
        await context.SaveChangesAsync();
        var afterCreate = ipc.RowVersion;

        ipc.Submit("ahmer.bilal");
        await context.SaveChangesAsync();
        var afterSubmit = ipc.RowVersion;

        ipc.Approve("engineer");
        await context.SaveChangesAsync();
        var afterApprove = ipc.RowVersion;

        Assert.NotEqual(afterCreate, afterSubmit);
        Assert.NotEqual(afterSubmit, afterApprove);
    }
}
