using Microsoft.EntityFrameworkCore;
using Modules.Finance.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Finance.IntegrationTests;

public class APInvoicePersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly InvoiceDate = new(2026, 7, 14);

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_invoice_with_tax_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        var vendorId = Guid.NewGuid();
        var expenseAccountId = Guid.NewGuid();
        var payableAccountId = Guid.NewGuid();
        var vatAccountId = Guid.NewGuid();
        var taxCodeId = Guid.NewGuid();
        var journalEntryId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var invoice = new APInvoice(
                "ahmer.bilal", vendorId, "INV-2026-0456", InvoiceDate, "Office supplies",
                expenseAccountId, payableAccountId, 1000m);
            invoice.SetTax(taxCodeId, 15m, vatAccountId);
            invoice.LinkJournalEntry(journalEntryId);
            invoice.AssignNumber("FIN-AP-2026-000001");
            writeContext.APInvoices.Add(invoice);
            await writeContext.SaveChangesAsync();
            id = invoice.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.APInvoices.FirstOrDefaultAsync(i => i.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("FIN-AP-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(vendorId, reloaded.VendorId);
        Assert.Equal("INV-2026-0456", reloaded.VendorInvoiceNumber);
        Assert.Equal(1000m, reloaded.NetAmount);
        Assert.Equal(15m, reloaded.TaxRate);
        Assert.Equal(150m, reloaded.TaxAmount);
        Assert.Equal(1150m, reloaded.GrossAmount);
        Assert.Equal(vatAccountId, reloaded.VatAccountId);
        Assert.Equal(journalEntryId, reloaded.LinkedJournalEntryId);
    }

    [Fact]
    public async Task Submit_approve_post_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var invoice = new APInvoice(
                "ahmer.bilal", Guid.NewGuid(), "INV-1", InvoiceDate, "Test", Guid.NewGuid(), Guid.NewGuid(), 500m);
            invoice.AssignNumber("FIN-AP-2026-000002");
            writeContext.APInvoices.Add(invoice);
            await writeContext.SaveChangesAsync();
            invoice.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            invoice.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            invoice.LinkJournalEntry(Guid.NewGuid());
            invoice.Post("finance.manager");
            await writeContext.SaveChangesAsync();
            id = invoice.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.APInvoices.FirstAsync(i => i.Id == id);
        Assert.Equal(BusinessObjectStatus.Posted, reloaded.Status);
        Assert.Equal(3, reloaded.RowVersion);
        Assert.NotNull(reloaded.LinkedJournalEntryId);
    }
}
