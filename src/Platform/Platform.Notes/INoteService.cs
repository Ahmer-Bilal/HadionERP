namespace Platform.Notes;

/// <summary>The single entry point modules call to add/retrieve/remove notes — mirrors
/// <c>Platform.Attachments.IAttachmentService</c>. Validation (non-empty, length ceiling) lives here so
/// every module gets the same rules.</summary>
public interface INoteService
{
    /// <summary>Throws <see cref="ArgumentException"/> if the text is empty/whitespace or over
    /// <see cref="NoteService.MaxTextLength"/>.</summary>
    Task<Note> AddAsync(
        string businessObjectType, Guid businessObjectId, string text, string createdBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> ListAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default);

    /// <summary>False if no note with that id exists.</summary>
    Task<bool> DeleteAsync(Guid noteId, CancellationToken cancellationToken = default);
}
