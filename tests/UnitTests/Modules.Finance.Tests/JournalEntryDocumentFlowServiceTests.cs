using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

public class JournalEntryDocumentFlowServiceTests
{
    private static readonly DateOnly PostingDate = new(2026, 7, 14);

    private static JournalEntryDocumentFlowService BuildService(
        out FakeJournalEntryRepository journalEntries, out FakeAPInvoiceRepository apInvoices, out FakeARInvoiceRepository arInvoices)
    {
        journalEntries = new FakeJournalEntryRepository();
        apInvoices = new FakeAPInvoiceRepository();
        arInvoices = new FakeARInvoiceRepository();
        return new JournalEntryDocumentFlowService(
            journalEntries, apInvoices, arInvoices, new FakePaymentRepository(), new FakeCustomerReceiptRepository());
    }

    private static JournalEntry PostedEntry(string? sourceType = null, Guid? sourceId = null)
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.AddLine(Guid.NewGuid(), null, 100, 0);
        entry.AddLine(Guid.NewGuid(), null, 0, 100);
        entry.AssignNumber("FIN-JE-2026-000001");
        if (sourceType is not null) entry.MarkSourceDocument(sourceType, sourceId);
        entry.Submit("ahmer.bilal");
        entry.Approve("finance.manager");
        entry.Post("finance.manager");
        return entry;
    }

    [Fact]
    public async Task A_manual_entry_with_no_history_has_only_the_current_node()
    {
        var service = BuildService(out var journalEntries, out _, out _);
        var entry = PostedEntry(JournalEntrySourceDocumentTypes.Manual);
        journalEntries.Add(entry);

        var nodes = await service.GetAsync(entry.Id);

        Assert.Single(nodes);
        Assert.Equal("JournalEntry", nodes[0].Kind);
        Assert.True(nodes[0].IsCurrent);
    }

    [Fact]
    public async Task An_APInvoice_sourced_entry_shows_the_invoice_node_and_a_pending_settlement()
    {
        var service = BuildService(out var journalEntries, out var apInvoices, out _);
        var invoice = new APInvoice("system", Guid.NewGuid(), "VINV-1", PostingDate, "Test", Guid.NewGuid(), Guid.NewGuid(), 500m);
        invoice.AssignNumber("FIN-AP-2026-000001");
        apInvoices.Add(invoice);
        var entry = PostedEntry(JournalEntrySourceDocumentTypes.APInvoice, invoice.Id);
        journalEntries.Add(entry);

        var nodes = await service.GetAsync(entry.Id);

        Assert.Equal(new[] { "APInvoice", "JournalEntry", "Payment" }, nodes.Select(n => n.Kind));
        Assert.Equal("Pending", nodes[2].Status);
        Assert.Null(nodes[2].DocumentId);
    }

    [Fact]
    public async Task An_IPC_raised_AP_invoice_surfaces_its_own_deeper_origin()
    {
        var service = BuildService(out var journalEntries, out var apInvoices, out _);
        var ipcId = Guid.NewGuid();
        var invoice = new APInvoice("system", Guid.NewGuid(), "IPC-2026-000001", PostingDate, "Test", Guid.NewGuid(), Guid.NewGuid(), 500m);
        invoice.AssignNumber("FIN-AP-2026-000002");
        invoice.MarkSourceDocument("Ipc", ipcId);
        apInvoices.Add(invoice);
        var entry = PostedEntry(JournalEntrySourceDocumentTypes.APInvoice, invoice.Id);
        journalEntries.Add(entry);

        var nodes = await service.GetAsync(entry.Id);

        Assert.Equal(new[] { "Ipc", "APInvoice", "JournalEntry", "Payment" }, nodes.Select(n => n.Kind));
        Assert.Equal(ipcId, nodes[0].DocumentId);
    }

    [Fact]
    public async Task A_reversal_mirror_shows_a_Reverses_node_pointing_at_the_original()
    {
        var service = BuildService(out var journalEntries, out _, out _);
        var original = PostedEntry(JournalEntrySourceDocumentTypes.Manual);
        journalEntries.Add(original);
        var mirror = new JournalEntry("finance.manager", PostingDate, "Reversal");
        mirror.AddLine(Guid.NewGuid(), null, 0, 100);
        mirror.AddLine(Guid.NewGuid(), null, 100, 0);
        mirror.AssignNumber("FIN-JE-2026-000002");
        mirror.MarkAsReversalOf(original.Id);
        journalEntries.Add(mirror);

        var nodes = await service.GetAsync(mirror.Id);

        Assert.Equal(new[] { "Reversal", "JournalEntry" }, nodes.Select(n => n.Kind));
        Assert.Equal("Reverses", nodes[0].Label);
        Assert.Equal(original.Id, nodes[0].DocumentId);
    }

    [Fact]
    public async Task An_original_entry_shows_a_Reversed_By_node_once_a_mirror_exists()
    {
        var service = BuildService(out var journalEntries, out _, out _);
        var original = PostedEntry(JournalEntrySourceDocumentTypes.Manual);
        journalEntries.Add(original);
        var mirror = new JournalEntry("finance.manager", PostingDate, "Reversal");
        mirror.AddLine(Guid.NewGuid(), null, 0, 100);
        mirror.AddLine(Guid.NewGuid(), null, 100, 0);
        mirror.AssignNumber("FIN-JE-2026-000002");
        mirror.MarkAsReversalOf(original.Id);
        journalEntries.Add(mirror);

        var nodes = await service.GetAsync(original.Id);

        Assert.Equal(new[] { "JournalEntry", "Reversal" }, nodes.Select(n => n.Kind));
        Assert.Equal("Reversed By", nodes[1].Label);
        Assert.Equal(mirror.Id, nodes[1].DocumentId);
    }
}
