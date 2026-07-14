namespace Modules.Procurement.Infrastructure;

/// <summary>The file bytes for one <see cref="Platform.Attachments.AttachmentMetadata"/>, stored in their
/// own table — a near-duplicate of Modules.MasterData's own copy, for the same "own schema, own kernel
/// plumbing" reason as <see cref="NumberRangeCounterEntity"/>. Nothing outside
/// <see cref="EfAttachmentRepository"/> ever sees this type.</summary>
internal sealed class AttachmentContentRow
{
    public Guid AttachmentId { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
