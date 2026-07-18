using Modules.Finance.Domain;
using Platform.Core;

namespace Modules.Finance.Tests;

public class JournalEntryTests
{
    private static readonly DateOnly PostingDate = new(2026, 7, 14);

    [Fact]
    public void A_new_entry_starts_in_draft_with_no_document_number_and_no_lines()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test entry");

        Assert.Equal(BusinessObjectStatus.Draft, entry.Status);
        Assert.Null(entry.DocumentNumber);
        Assert.Empty(entry.Lines);
        Assert.False(entry.IsBalanced); // zero lines is incomplete, not "balanced by vacuous truth"
    }

    [Fact]
    public void Blank_description_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new JournalEntry("ahmer.bilal", PostingDate, ""));
    }

    [Fact]
    public void AddLine_rejects_a_line_with_both_debit_and_credit_positive()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        Assert.Throws<ArgumentException>(() => entry.AddLine(Guid.NewGuid(), null, 100, 100));
    }

    [Fact]
    public void AddLine_rejects_a_line_with_neither_debit_nor_credit()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        Assert.Throws<ArgumentException>(() => entry.AddLine(Guid.NewGuid(), null, 0, 0));
    }

    [Fact]
    public void AddLine_rejects_negative_amounts()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        Assert.Throws<ArgumentException>(() => entry.AddLine(Guid.NewGuid(), null, -50, 0));
    }

    [Fact]
    public void Two_equal_debit_and_credit_lines_balance()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.AddLine(Guid.NewGuid(), null, 100, 0);
        entry.AddLine(Guid.NewGuid(), null, 0, 100);

        Assert.Equal(100, entry.TotalDebits);
        Assert.Equal(100, entry.TotalCredits);
        Assert.True(entry.IsBalanced);
    }

    [Fact]
    public void Unequal_debit_and_credit_lines_do_not_balance()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.AddLine(Guid.NewGuid(), null, 100, 0);
        entry.AddLine(Guid.NewGuid(), null, 0, 50);

        Assert.False(entry.IsBalanced);
    }

    [Fact]
    public void AddLine_after_submit_is_rejected()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.AddLine(Guid.NewGuid(), null, 100, 0);
        entry.AddLine(Guid.NewGuid(), null, 0, 100);
        entry.AssignNumber("FIN-JE-2026-000001");
        entry.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => entry.AddLine(Guid.NewGuid(), null, 10, 0));
    }

    [Fact]
    public void Post_refuses_an_unbalanced_entry()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.AddLine(Guid.NewGuid(), null, 100, 0);
        entry.AddLine(Guid.NewGuid(), null, 0, 50);
        entry.AssignNumber("FIN-JE-2026-000001");
        entry.Submit("ahmer.bilal");
        entry.Approve("finance.manager");

        Assert.Throws<InvalidOperationException>(() => entry.Post("finance.manager"));
    }

    [Fact]
    public void Full_lifecycle_draft_to_posted_to_reversed_the_first_real_use_of_this_path()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.AddLine(Guid.NewGuid(), null, 100, 0);
        entry.AddLine(Guid.NewGuid(), null, 0, 100);
        entry.AssignNumber("FIN-JE-2026-000001");

        entry.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, entry.Status);

        entry.Approve("finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, entry.Status);

        entry.Post("finance.manager");
        Assert.Equal(BusinessObjectStatus.Posted, entry.Status);

        entry.Reverse("finance.manager");
        Assert.Equal(BusinessObjectStatus.Reversed, entry.Status);
    }

    [Fact]
    public void MarkAsReversalOf_sets_the_link()
    {
        var original = Guid.NewGuid();
        var mirror = new JournalEntry("finance.manager", PostingDate, "Reversal");
        mirror.MarkAsReversalOf(original);

        Assert.Equal(original, mirror.ReversalOfEntryId);
    }

    [Fact]
    public void MarkSourceDocument_sets_both_fields()
    {
        var sourceId = Guid.NewGuid();
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.MarkSourceDocument("APInvoice", sourceId);

        Assert.Equal("APInvoice", entry.SourceDocumentType);
        Assert.Equal(sourceId, entry.SourceDocumentId);
    }

    [Fact]
    public void MarkSourceDocument_allows_a_null_id_for_Manual_entries()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.MarkSourceDocument("Manual", null);

        Assert.Equal("Manual", entry.SourceDocumentType);
        Assert.Null(entry.SourceDocumentId);
    }

    [Fact]
    public void MarkSourceDocument_rejects_a_blank_type()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        Assert.Throws<ArgumentException>(() => entry.MarkSourceDocument("", null));
    }

    [Fact]
    public void MarkSourceDocument_after_submit_is_rejected()
    {
        var entry = new JournalEntry("ahmer.bilal", PostingDate, "Test");
        entry.AddLine(Guid.NewGuid(), null, 100, 0);
        entry.AddLine(Guid.NewGuid(), null, 0, 100);
        entry.AssignNumber("FIN-JE-2026-000001");
        entry.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => entry.MarkSourceDocument("Manual", null));
    }
}
