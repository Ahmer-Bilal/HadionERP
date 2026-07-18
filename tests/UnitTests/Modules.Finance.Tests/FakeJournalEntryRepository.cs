using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

internal sealed class FakeJournalEntryRepository : IJournalEntryRepository
{
    private readonly Dictionary<Guid, JournalEntry> _entries = new();

    public Task<JournalEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.GetValueOrDefault(id));

    public Task<IReadOnlyList<JournalEntry>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JournalEntry>>(
            _entries.Values.OrderByDescending(e => e.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_entries.Count);

    public Task<IReadOnlyList<JournalEntry>> ListPostedAsync(DateOnly postedOnOrBefore, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JournalEntry>>(
            _entries.Values.Where(e => e.Status == Platform.Core.BusinessObjectStatus.Posted && e.PostingDate <= postedOnOrBefore).ToList());

    public Task<IReadOnlyList<JournalEntry>> ListManualByPostingDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JournalEntry>>(
            _entries.Values.Where(e => e.SourceDocumentType == JournalEntrySourceDocumentTypes.Manual && e.PostingDate >= start && e.PostingDate <= end).ToList());

    public Task<JournalEntry?> FindReversalOfAsync(Guid originalEntryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.Values.FirstOrDefault(e => e.ReversalOfEntryId == originalEntryId));

    public void Add(JournalEntry entry) => _entries[entry.Id] = entry;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
