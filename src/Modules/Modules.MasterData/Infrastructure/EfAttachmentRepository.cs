using Microsoft.EntityFrameworkCore;
using Platform.Attachments;

namespace Modules.MasterData.Infrastructure;

public sealed class EfAttachmentRepository : IAttachmentRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfAttachmentRepository(MasterDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AttachmentMetadata metadata, byte[] content, CancellationToken cancellationToken = default)
    {
        _dbContext.Attachments.Add(metadata);
        _dbContext.AttachmentContents.Add(new AttachmentContentRow { AttachmentId = metadata.Id, Content = content });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttachmentMetadata>> ListForAsync(
        string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default) =>
        await _dbContext.Attachments
            .Where(a => a.BusinessObjectType == businessObjectType && a.BusinessObjectId == businessObjectId)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(cancellationToken);

    public Task<AttachmentMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Attachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<byte[]?> GetContentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.AttachmentContents.FirstOrDefaultAsync(c => c.AttachmentId == id, cancellationToken);
        return row?.Content;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metadata = await _dbContext.Attachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (metadata is null)
        {
            return false;
        }

        // AttachmentContentRow is removed by the database via ON DELETE CASCADE (see
        // MasterDataDbContext's Fluent API configuration) — no need to load and remove it here.
        _dbContext.Attachments.Remove(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
