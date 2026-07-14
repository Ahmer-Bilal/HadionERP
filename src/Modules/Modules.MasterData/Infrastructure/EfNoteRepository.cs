using Microsoft.EntityFrameworkCore;
using Platform.Notes;

namespace Modules.MasterData.Infrastructure;

public sealed class EfNoteRepository : INoteRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfNoteRepository(MasterDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        _dbContext.Notes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Note>> ListForAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default) =>
        await _dbContext.Notes
            .Where(n => n.BusinessObjectType == businessObjectType && n.BusinessObjectId == businessObjectId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Notes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await _dbContext.Notes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (note is null)
        {
            return false;
        }

        _dbContext.Notes.Remove(note);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
