using Microsoft.EntityFrameworkCore;
using Modules.Finance.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Finance.IntegrationTests;

public class JournalEntryPersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly PostingDate = new(2026, 7, 14);

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_entry_with_its_lines_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        var cashAccountId = Guid.NewGuid();
        var revenueAccountId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var entry = new JournalEntry("ahmer.bilal", PostingDate, "Consulting revenue received in cash");
            entry.AddLine(cashAccountId, null, 1000m, 0m, "Cash received");
            entry.AddLine(revenueAccountId, null, 0m, 1000m, "Consulting fee");
            entry.AssignNumber("FIN-JE-2026-000001");
            writeContext.JournalEntries.Add(entry);
            await writeContext.SaveChangesAsync();
            id = entry.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.JournalEntries.Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("FIN-JE-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal("Consulting revenue received in cash", reloaded.Description);
        Assert.Equal(PostingDate, reloaded.PostingDate);
        Assert.Equal(2, reloaded.Lines.Count);
        Assert.True(reloaded.IsBalanced);
        Assert.Equal(1000m, reloaded.TotalDebits);
        Assert.Equal(1000m, reloaded.TotalCredits);

        var cashLine = reloaded.Lines.Single(l => l.GLAccountId == cashAccountId);
        Assert.Equal(1000m, cashLine.DebitAmount);
        Assert.Equal("Cash received", cashLine.LineDescription);
    }

    [Fact]
    public async Task Submit_approve_post_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
            entry.AddLine(Guid.NewGuid(), null, 100m, 0m);
            entry.AddLine(Guid.NewGuid(), null, 0m, 100m);
            entry.AssignNumber("FIN-JE-2026-000002");
            writeContext.JournalEntries.Add(entry);
            await writeContext.SaveChangesAsync();
            entry.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            entry.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            entry.Post("finance.manager");
            await writeContext.SaveChangesAsync();
            id = entry.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.JournalEntries.FirstAsync(e => e.Id == id);
        Assert.Equal(BusinessObjectStatus.Posted, reloaded.Status);
        Assert.Equal(3, reloaded.RowVersion); // Submit, Approve, Post = three transitions
    }

    [Fact]
    public async Task Deleting_an_entry_cascades_to_its_lines()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
            entry.AddLine(Guid.NewGuid(), null, 100m, 0m);
            entry.AddLine(Guid.NewGuid(), null, 0m, 100m);
            entry.AssignNumber("FIN-JE-2026-000003");
            writeContext.JournalEntries.Add(entry);
            await writeContext.SaveChangesAsync();
            id = entry.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var entry = await deleteContext.JournalEntries.FirstAsync(e => e.Id == id);
            deleteContext.JournalEntries.Remove(entry);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingLineCount = await readContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM finance.journal_lines")
            .SingleAsync();
        Assert.Equal(0, remainingLineCount);
    }

    [Fact]
    public async Task A_reversal_entry_persists_its_link_to_the_original()
    {
        Guid originalId;
        Guid mirrorId;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var original = new JournalEntry("ahmer.bilal", PostingDate, "Original");
            original.AddLine(Guid.NewGuid(), null, 100m, 0m);
            original.AddLine(Guid.NewGuid(), null, 0m, 100m);
            original.AssignNumber("FIN-JE-2026-000004");
            writeContext.JournalEntries.Add(original);
            await writeContext.SaveChangesAsync();
            originalId = original.Id;

            var mirror = new JournalEntry("finance.manager", PostingDate, "Reversal of FIN-JE-2026-000004");
            mirror.MarkAsReversalOf(originalId);
            mirror.AssignNumber("FIN-JE-2026-000005");
            writeContext.JournalEntries.Add(mirror);
            await writeContext.SaveChangesAsync();
            mirrorId = mirror.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloadedMirror = await readContext.JournalEntries.FirstAsync(e => e.Id == mirrorId);
        Assert.Equal(originalId, reloadedMirror.ReversalOfEntryId);
    }

    [Fact]
    public async Task SourceDocumentType_and_Id_persist_through_a_fresh_DbContext()
    {
        Guid id;
        var sourceId = Guid.NewGuid();
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var entry = new JournalEntry("system", PostingDate, "AP Invoice AP-2026-000001");
            entry.AddLine(Guid.NewGuid(), null, 100m, 0m);
            entry.AddLine(Guid.NewGuid(), null, 0m, 100m);
            entry.MarkSourceDocument("APInvoice", sourceId);
            entry.AssignNumber("FIN-JE-2026-000006");
            writeContext.JournalEntries.Add(entry);
            await writeContext.SaveChangesAsync();
            id = entry.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.JournalEntries.FirstAsync(e => e.Id == id);
        Assert.Equal("APInvoice", reloaded.SourceDocumentType);
        Assert.Equal(sourceId, reloaded.SourceDocumentId);
    }
}
