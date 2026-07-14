namespace Modules.MasterData.Infrastructure;

/// <summary>
/// The file bytes for one <see cref="Platform.Attachments.AttachmentMetadata"/>, stored in their own
/// table so that listing/loading metadata (a common, cheap read) never has to materialize file content —
/// see <c>AttachmentMetadata</c>'s own doc comment. Purely a storage-layer concern: nothing outside
/// <see cref="EfAttachmentRepository"/> ever sees this type.
/// </summary>
internal sealed class AttachmentContentRow
{
    public Guid AttachmentId { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
