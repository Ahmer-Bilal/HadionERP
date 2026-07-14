using Microsoft.EntityFrameworkCore;
using Platform.Attachments;

namespace Modules.Procurement.Infrastructure;

/// <summary>Implements <see cref="IAttachmentRepository"/> against this module's own
/// <see cref="ProcurementDbContext"/> — a near-duplicate of Modules.MasterData's own copy, for the "each
/// module owns its own schema" reason documented on <see cref="NumberRangeCounterEntity"/>.</summary>
public sealed class EfAttachmentRepository : IAttachmentRepository
{
    private readonly ProcurementDbContext _dbContext;

    public EfAttachmentRepository(ProcurementDbContext dbContext)
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

        _dbContext.Attachments.Remove(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
