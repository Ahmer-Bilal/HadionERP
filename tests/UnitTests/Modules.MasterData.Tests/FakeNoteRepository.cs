using Platform.Notes;

namespace Modules.MasterData.Tests;

internal sealed class FakeNoteRepository : INoteRepository
{
    private readonly Dictionary<Guid, Note> _notes = new();

    public Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        _notes[note.Id] = note;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Note>> ListForAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Note>>(_notes.Values
            .Where(n => n.BusinessObjectType == businessObjectType && n.BusinessObjectId == businessObjectId)
            .OrderBy(n => n.CreatedAt)
            .ToList());

    public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_notes.GetValueOrDefault(id));

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_notes.Remove(id));
}
