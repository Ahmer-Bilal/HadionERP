namespace Platform.Attachments;

/// <summary>
/// The persistence port for attachments — mirrors <c>Platform.Workflow.IWorkflowInstanceRepository</c>'s
/// pattern (a storage-agnostic kernel interface, implemented per-module against that module's own real
/// database). <see cref="Platform.Attachments"/> itself has no database dependency; the first real
/// implementation, <c>Modules.MasterData.Infrastructure.EfAttachmentRepository</c>, stores file bytes as a
/// separate row from the metadata specifically so listing attachments never has to load them (see
/// <see cref="AttachmentMetadata"/>'s own doc comment).
/// </summary>
public interface IAttachmentRepository
{
    Task AddAsync(AttachmentMetadata metadata, byte[] content, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttachmentMetadata>> ListForAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default);

    Task<AttachmentMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default);

    Task<byte[]?> GetContentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no attachment with that id exists — callers decide whether that's a 404
    /// or a no-op, this port only reports what actually happened.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
