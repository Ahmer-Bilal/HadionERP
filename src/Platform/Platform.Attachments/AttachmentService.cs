namespace Platform.Attachments;

public sealed class AttachmentService : IAttachmentService
{
    /// <summary>10 MB — a reasonable ceiling for the document scans/certificates this platform's first
    /// real use cases need (CR copies, ISO certificates, bank letters); revisit if a future module needs
    /// larger files (e.g. CAD drawings) via its own configured override rather than raising this globally.</summary>
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    /// <summary>An allowlist, not a denylist — rejecting executables by trying to enumerate every
    /// dangerous extension is a losing game; only admitting known-safe document/image types is not.
    /// Deliberately does not admit any executable, script, or macro-capable format.</summary>
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };

    private readonly IAttachmentRepository _repository;

    public AttachmentService(IAttachmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<AttachmentMetadata> UploadAsync(
        string businessObjectType, Guid businessObjectId, string fileName, string contentType, byte[] content,
        string uploadedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.", nameof(fileName));
        }

        if (content.Length == 0)
        {
            throw new ArgumentException("The file is empty.", nameof(content));
        }

        if (content.Length > MaxSizeBytes)
        {
            throw new ArgumentException($"The file exceeds the {MaxSizeBytes / (1024 * 1024)} MB limit.", nameof(content));
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException(
                $"File type '{contentType}' is not allowed. Allowed types: PDF, PNG, JPEG, Word, Excel.", nameof(contentType));
        }

        var metadata = new AttachmentMetadata(businessObjectType, businessObjectId, fileName, contentType, content.Length, uploadedBy);
        await _repository.AddAsync(metadata, content, cancellationToken);
        return metadata;
    }

    public Task<IReadOnlyList<AttachmentMetadata>> ListAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default) =>
        _repository.ListForAsync(businessObjectType, businessObjectId, cancellationToken);

    public async Task<(AttachmentMetadata Metadata, byte[] Content)?> DownloadAsync(
        Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var metadata = await _repository.GetMetadataAsync(attachmentId, cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        var content = await _repository.GetContentAsync(attachmentId, cancellationToken);
        return content is null ? null : (metadata, content);
    }

    public Task<bool> DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(attachmentId, cancellationToken);
}
