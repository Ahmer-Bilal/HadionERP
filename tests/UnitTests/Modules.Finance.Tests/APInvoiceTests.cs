using Modules.Finance.Domain;
using Platform.Core;

namespace Modules.Finance.Tests;

public class APInvoiceTests
{
    private static readonly DateOnly InvoiceDate = new(2026, 7, 14);

    [Fact]
    public void A_new_invoice_starts_in_draft_with_no_document_number_and_zero_tax()
    {
        var invoice = new APInvoice(
            "ahmer.bilal", Guid.NewGuid(), "INV-2026-0456", InvoiceDate, "Office supplies",
            Guid.NewGuid(), Guid.NewGuid(), 1000m);

        Assert.Equal(BusinessObjectStatus.Draft, invoice.Status);
        Assert.Null(invoice.DocumentNumber);
        Assert.Equal(1000m, invoice.NetAmount);
        Assert.Equal(0m, invoice.TaxRate);
        Assert.Equal(0m, invoice.TaxAmount);
        Assert.Equal(1000m, invoice.GrossAmount);
        Assert.Null(invoice.LinkedJournalEntryId);
    }

    [Fact]
    public void Blank_vendor_invoice_number_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new APInvoice(
            "ahmer.bilal", Guid.NewGuid(), "", InvoiceDate, "Test", Guid.NewGuid(), Guid.NewGuid(), 100m));
    }

    [Fact]
    public void Zero_or_negative_net_amount_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new APInvoice(
            "ahmer.bilal", Guid.NewGuid(), "INV-1", InvoiceDate, "Test", Guid.NewGuid(), Guid.NewGuid(), 0m));
        Assert.Throws<ArgumentException>(() => new APInvoice(
            "ahmer.bilal", Guid.NewGuid(), "INV-1", InvoiceDate, "Test", Guid.NewGuid(), Guid.NewGuid(), -50m));
    }

    [Fact]
    public void SetTax_computes_TaxAmount_and_GrossAmount_correctly()
    {
        var invoice = new APInvoice(
            "ahmer.bilal", Guid.NewGuid(), "INV-2026-0456", InvoiceDate, "Office supplies",
            Guid.NewGuid(), Guid.NewGuid(), 1000m);

        invoice.SetTax(Guid.NewGuid(), 15m, Guid.NewGuid());

        Assert.Equal(15m, invoice.TaxRate);
        Assert.Equal(150m, invoice.TaxAmount);
        Assert.Equal(1150m, invoice.GrossAmount);
    }

    [Fact]
    public void SetTax_rejects_a_rate_outside_0_to_100()
    {
        var invoice = new APInvoice(
            "ahmer.bilal", Guid.NewGuid(), "INV-1", InvoiceDate, "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);

        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.SetTax(Guid.NewGuid(), 150m, Guid.NewGuid()));
    }

    [Fact]
    public void Full_lifecycle_draft_to_posted_to_reversed()
    {
        var invoice = new APInvoice(
            "ahmer.bilal", Guid.NewGuid(), "INV-1", InvoiceDate, "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        invoice.AssignNumber("FIN-AP-2026-000001");

        invoice.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, invoice.Status);
        invoice.Approve("finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, invoice.Status);
        invoice.Post("finance.manager");
        Assert.Equal(BusinessObjectStatus.Posted, invoice.Status);
        invoice.Reverse("finance.manager");
        Assert.Equal(BusinessObjectStatus.Reversed, invoice.Status);
    }

    [Fact]
    public void LinkJournalEntry_sets_the_link()
    {
        var invoice = new APInvoice(
            "ahmer.bilal", Guid.NewGuid(), "INV-1", InvoiceDate, "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        var journalEntryId = Guid.NewGuid();
        invoice.LinkJournalEntry(journalEntryId);

        Assert.Equal(journalEntryId, invoice.LinkedJournalEntryId);
    }
}
