namespace Platform.Attachments;

/// <summary>
/// The single entry point modules call to attach/retrieve/remove files — mirrors how
/// <c>Platform.Audit.IAuditRecorder</c> is the friendly facade over its own repository port. Modules never
/// construct <see cref="AttachmentMetadata"/> or call <see cref="IAttachmentRepository"/> directly; this is
/// where upload validation (size limit, allowed content types) lives, consistently for every module.
/// </summary>
public interface IAttachmentService
{
    /// <summary>Validates and stores a new attachment. Throws <see cref="ArgumentException"/> if the file
    /// is empty, over <see cref="AttachmentService.MaxSizeBytes"/>, or not an allowed content type.</summary>
    Task<AttachmentMetadata> UploadAsync(
        string businessObjectType, Guid businessObjectId, string fileName, string contentType, byte[] content,
        string uploadedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttachmentMetadata>> ListAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default);

    /// <summary>Null if no attachment with that id exists.</summary>
    Task<(AttachmentMetadata Metadata, byte[] Content)?> DownloadAsync(Guid attachmentId, CancellationToken cancellationToken = default);

    /// <summary>False if no attachment with that id exists.</summary>
    Task<bool> DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}
