namespace Platform.Attachments.Tests;

internal sealed class FakeAttachmentRepository : IAttachmentRepository
{
    private readonly Dictionary<Guid, (AttachmentMetadata Metadata, byte[] Content)> _attachments = new();

    public Task AddAsync(AttachmentMetadata metadata, byte[] content, CancellationToken cancellationToken = default)
    {
        _attachments[metadata.Id] = (metadata, content);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AttachmentMetadata>> ListForAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AttachmentMetadata>>(_attachments.Values
            .Select(a => a.Metadata)
            .Where(m => m.BusinessObjectType == businessObjectType && m.BusinessObjectId == businessObjectId)
            .OrderBy(m => m.UploadedAt)
            .ToList());

    public Task<AttachmentMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_attachments.TryGetValue(id, out var entry) ? entry.Metadata : null);

    public Task<byte[]?> GetContentAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_attachments.TryGetValue(id, out var entry) ? entry.Content : null);

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_attachments.Remove(id));
}
