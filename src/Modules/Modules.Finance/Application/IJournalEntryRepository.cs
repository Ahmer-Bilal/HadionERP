using Modules.Finance.Domain;

namespace Modules.Finance.Application;

/// <summary>The persistence port for Journal Entries — same dependency-inversion shape as
/// Modules.MasterData's repository ports.</summary>
public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JournalEntry>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(JournalEntry entry);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
