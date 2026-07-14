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

    public void Add(JournalEntry entry) => _entries[entry.Id] = entry;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
