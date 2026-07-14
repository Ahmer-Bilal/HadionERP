using Microsoft.EntityFrameworkCore;
using Modules.Procurement.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Procurement.IntegrationTests;

public class RequestForQuotationPersistenceTests : IAsyncLifetime
{
    private static readonly Guid PurchaseRequisitionId = Guid.NewGuid();

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_rfq_with_lines_invited_vendors_and_quotes_reads_back_identically()
    {
        Guid id;
        var itemId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var prLineId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Cement quotes", new DateOnly(2026, 8, 1));
            var line = rfq.AddLine(prLineId, itemId, 100);
            rfq.InviteVendor(vendorId);
            rfq.AssignNumber("PROC-RFQ-2026-000001");
            writeContext.RequestsForQuotation.Add(rfq);
            await writeContext.SaveChangesAsync();

            rfq.Submit("ahmer.bilal");
            rfq.RecordVendorQuote(vendorId, line.Id, 27.5m);
            await writeContext.SaveChangesAsync();
            id = rfq.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.RequestsForQuotation
            .Include(r => r.Lines).Include(r => r.InvitedVendors).Include(r => r.VendorQuoteLines)
            .FirstOrDefaultAsync(r => r.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("PROC-RFQ-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Submitted, reloaded.Status);
        Assert.Single(reloaded.Lines);
        Assert.Equal(itemId, reloaded.Lines.Single().ItemId);
        Assert.Equal(prLineId, reloaded.Lines.Single().PurchaseRequisitionLineId);
        Assert.Single(reloaded.InvitedVendors);
        Assert.Equal(vendorId, reloaded.InvitedVendors.Single().VendorId);
        Assert.Single(reloaded.VendorQuoteLines);
        Assert.Equal(27.5m, reloaded.VendorQuoteLines.Single().QuotedUnitPrice);
    }

    [Fact]
    public async Task Deleting_an_rfq_cascades_to_its_lines_invited_vendors_and_quotes()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test");
            var line = rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10);
            var vendorId = Guid.NewGuid();
            rfq.InviteVendor(vendorId);
            rfq.AssignNumber("PROC-RFQ-2026-000002");
            writeContext.RequestsForQuotation.Add(rfq);
            await writeContext.SaveChangesAsync();
            rfq.Submit("ahmer.bilal");
            rfq.RecordVendorQuote(vendorId, line.Id, 10);
            await writeContext.SaveChangesAsync();
            id = rfq.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var rfq = await deleteContext.RequestsForQuotation.FirstAsync(r => r.Id == id);
            deleteContext.RequestsForQuotation.Remove(rfq);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLines = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM procurement.rfq_lines").SingleAsync();
        var remainingVendors = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM procurement.rfq_invited_vendors").SingleAsync();
        var remainingQuotes = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM procurement.rfq_vendor_quote_lines").SingleAsync();
        Assert.Equal(0, remainingLines);
        Assert.Equal(0, remainingVendors);
        Assert.Equal(0, remainingQuotes);
    }
}
