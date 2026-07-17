using Microsoft.EntityFrameworkCore;
using Modules.Construction.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Construction.IntegrationTests;

public class MeasurementSheetPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateOnly PeriodStart = new(2026, 7, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 7, 31);

    [Fact]
    public async Task A_saved_measurement_sheet_with_lines_reads_back_identically()
    {
        var projectId = Guid.NewGuid();
        var contractLineId = Guid.NewGuid();
        Guid id;
        Guid lineId;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var sheet = new MeasurementSheet(
                "ahmer.bilal", projectId, CommercialDocumentType.Contract, Guid.NewGuid(), PeriodStart, PeriodEnd, "First period");
            var line = sheet.AddLine(contractLineId, 40m, "On track");
            sheet.AssignNumber("CON-MEAS-2026-000001");
            writeContext.MeasurementSheets.Add(sheet);
            await writeContext.SaveChangesAsync();

            sheet.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal> { [line.Id] = 35m });
            sheet.Approve("engineer");
            await writeContext.SaveChangesAsync();
            id = sheet.Id;
            lineId = line.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.MeasurementSheets.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("CON-MEAS-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal(CommercialDocumentType.Contract, reloaded.CommercialDocumentType);
        Assert.Equal(PeriodStart, reloaded.PeriodStart);
        Assert.Equal(PeriodEnd, reloaded.PeriodEnd);
        Assert.Equal("First period", reloaded.Notes);
        var reloadedLine = Assert.Single(reloaded.Lines);
        Assert.Equal(lineId, reloadedLine.Id);
        Assert.Equal(contractLineId, reloadedLine.CommercialDocumentLineId);
        Assert.Equal(40m, reloadedLine.QuantitySubmitted);
        Assert.Equal(35m, reloadedLine.QuantityCertified);
        Assert.Equal("On track", reloadedLine.Remarks);
    }

    [Fact]
    public async Task Deleting_a_measurement_sheet_cascades_to_its_lines()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var sheet = new MeasurementSheet(
                "ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Subcontract, Guid.NewGuid(), PeriodStart, PeriodEnd, null);
            sheet.AddLine(Guid.NewGuid(), 10m, null);
            sheet.AssignNumber("CON-MEAS-2026-000002");
            writeContext.MeasurementSheets.Add(sheet);
            await writeContext.SaveChangesAsync();
            id = sheet.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var sheet = await deleteContext.MeasurementSheets.FirstAsync(s => s.Id == id);
            deleteContext.MeasurementSheets.Remove(sheet);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLines = await readContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM construction.measurement_lines").SingleAsync();
        Assert.Equal(0, remainingLines);
    }

    [Fact]
    public async Task RowVersion_increments_across_real_transitions()
    {
        await using var context = TestDatabase.CreateContext();
        var sheet = new MeasurementSheet(
            "ahmer.bilal", Guid.NewGuid(), CommercialDocumentType.Contract, Guid.NewGuid(), PeriodStart, PeriodEnd, null);
        var line = sheet.AddLine(Guid.NewGuid(), 10m, null);
        sheet.AssignNumber("CON-MEAS-2026-000003");
        context.MeasurementSheets.Add(sheet);
        await context.SaveChangesAsync();
        var afterCreate = sheet.RowVersion;

        sheet.Submit("ahmer.bilal");
        await context.SaveChangesAsync();
        var afterSubmit = sheet.RowVersion;

        sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal> { [line.Id] = 10m });
        sheet.Approve("engineer");
        await context.SaveChangesAsync();
        var afterApprove = sheet.RowVersion;

        Assert.NotEqual(afterCreate, afterSubmit);
        Assert.NotEqual(afterSubmit, afterApprove);
    }
}
