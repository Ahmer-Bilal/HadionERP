namespace Platform.Notes;

/// <summary>
/// The persistence port for notes — same storage-agnostic-kernel-port pattern as
/// <c>Platform.Attachments.IAttachmentRepository</c>/<c>Platform.Workflow.IWorkflowInstanceRepository</c>.
/// </summary>
public interface INoteRepository
{
    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> ListForAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default);

    Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no note with that id exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
