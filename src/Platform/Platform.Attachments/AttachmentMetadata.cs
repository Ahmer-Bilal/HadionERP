namespace Platform.Attachments;

/// <summary>
/// One uploaded file's metadata, attached to a Business Object identified by a flat
/// (<see cref="BusinessObjectType"/>, <see cref="BusinessObjectId"/>) pair — the same polymorphic-link
/// shape <c>Platform.Workflow.WorkflowInstance</c> already uses for its own Business Object association,
/// not <c>Platform.Core.BusinessObjectReference</c> (that type also carries a <c>RelationKind</c> for
/// typed cross-document links, which doesn't apply here — an attachment always belongs to exactly the one
/// record it was uploaded against).
///
/// The file's bytes are NOT a property here on purpose — listing a Business Object's attachments (a
/// perfectly ordinary read) must never materialize every file's full content just to show a filename and
/// size. Fetch bytes separately via <see cref="IAttachmentRepository.GetContentAsync"/>.
/// </summary>
public sealed class AttachmentMetadata
{
    public Guid Id { get; private set; }
    public string BusinessObjectType { get; private set; }
    public Guid BusinessObjectId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public string UploadedBy { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    internal AttachmentMetadata(
        string businessObjectType, Guid businessObjectId, string fileName, string contentType, long sizeBytes, string uploadedBy)
    {
        Id = Guid.NewGuid();
        BusinessObjectType = businessObjectType;
        BusinessObjectId = businessObjectId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedBy = uploadedBy;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reserved for ORM materialization — mirrors <c>Platform.Core.BusinessObject</c>'s own
    /// parameterless constructor. Never call from application code; use <see cref="IAttachmentService.UploadAsync"/>.</summary>
    private AttachmentMetadata()
    {
        BusinessObjectType = null!;
        FileName = null!;
        ContentType = null!;
        UploadedBy = null!;
    }
}
