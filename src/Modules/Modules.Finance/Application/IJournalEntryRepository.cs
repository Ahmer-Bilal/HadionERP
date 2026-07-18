using Modules.Finance.Domain;

namespace Modules.Finance.Application;

/// <summary>The persistence port for Journal Entries — same dependency-inversion shape as
/// Modules.MasterData's repository ports.</summary>
public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JournalEntry>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Every Posted entry with <c>PostingDate &lt;= postedOnOrBefore</c>, lines included — the one
    /// query a financial report (Trial Balance, and later Income Statement/Balance Sheet) needs: everything
    /// that has ever had a real ledger effect up to a given date. Draft/Submitted/Approved/Rejected entries
    /// are excluded since they haven't posted to the ledger yet, and a Reversed entry's own lines stay
    /// included (its balance-undoing mirror entry is a separate Posted row, not a flag on this one) — the
    /// same "correct by reversal, not by deletion" reasoning as everywhere else in this platform.</summary>
    Task<IReadOnlyList<JournalEntry>> ListPostedAsync(DateOnly postedOnOrBefore, CancellationToken cancellationToken = default);

    /// <summary>Every Manual entry (<c>SourceDocumentType == "Manual"</c>) with <c>PostingDate</c> in range,
    /// any status — the real "manual journals this period" list <c>ClosingActivityService</c> builds its
    /// Journal Review checklist step from.</summary>
    Task<IReadOnlyList<JournalEntry>> ListManualByPostingDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    /// <summary>The mirror entry that reverses <paramref name="originalEntryId"/>, if any — the reverse
    /// direction of <see cref="JournalEntry.ReversalOfEntryId"/> (which only ever points backward from a
    /// mirror to its original). Powers the Document Flow panel's "Reversed by" node.</summary>
    Task<JournalEntry?> FindReversalOfAsync(Guid originalEntryId, CancellationToken cancellationToken = default);

    void Add(JournalEntry entry);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
