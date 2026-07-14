namespace Platform.Notes;

public sealed class NoteService : INoteService
{
    /// <summary>A generous ceiling for a free-text note — long enough for a real explanation, short
    /// enough that a note stays a note and doesn't become a place to paste a whole document (that's what
    /// Attachments is for).</summary>
    public const int MaxTextLength = 2000;

    private readonly INoteRepository _repository;

    public NoteService(INoteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Note> AddAsync(
        string businessObjectType, Guid businessObjectId, string text, string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Note text is required.", nameof(text));
        }

        if (text.Length > MaxTextLength)
        {
            throw new ArgumentException($"Note text exceeds the {MaxTextLength}-character limit.", nameof(text));
        }

        var note = new Note(businessObjectType, businessObjectId, text, createdBy);
        await _repository.AddAsync(note, cancellationToken);
        return note;
    }

    public Task<IReadOnlyList<Note>> ListAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default) =>
        _repository.ListForAsync(businessObjectType, businessObjectId, cancellationToken);

    public Task<bool> DeleteAsync(Guid noteId, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(noteId, cancellationToken);
}
