using Microsoft.EntityFrameworkCore;
using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Infrastructure;

public sealed class EfJournalEntryRepository : IJournalEntryRepository
{
    private readonly FinanceDbContext _dbContext;

    public EfJournalEntryRepository(FinanceDbContext dbContext) => _dbContext = dbContext;

    // Tracked (not AsNoTracking): Submit/Approve/Post/Reverse all load via GetAsync then mutate + Save, so
    // EF must observe the entity for changes — same rationale as every Modules.MasterData repository.
    // Lines are always needed alongside the entry (balance/posting can't be checked without them), so
    // GetAsync always Includes them.
    public Task<JournalEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.JournalEntries.Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JournalEntry>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.JournalEntries.AsNoTracking().Include(e => e.Lines)
            .OrderByDescending(e => e.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.JournalEntries.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<JournalEntry>> ListPostedAsync(DateOnly postedOnOrBefore, CancellationToken cancellationToken = default) =>
        await _dbContext.JournalEntries.AsNoTracking().Include(e => e.Lines)
            .Where(e => e.Status == Platform.Core.BusinessObjectStatus.Posted && e.PostingDate <= postedOnOrBefore)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<JournalEntry>> ListManualByPostingDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        await _dbContext.JournalEntries.AsNoTracking()
            .Where(e => e.SourceDocumentType == JournalEntrySourceDocumentTypes.Manual && e.PostingDate >= start && e.PostingDate <= end)
            .ToListAsync(cancellationToken);

    public Task<JournalEntry?> FindReversalOfAsync(Guid originalEntryId, CancellationToken cancellationToken = default) =>
        _dbContext.JournalEntries.AsNoTracking().Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.ReversalOfEntryId == originalEntryId, cancellationToken);

    public void Add(JournalEntry entry) => _dbContext.JournalEntries.Add(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
