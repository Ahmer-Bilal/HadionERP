using Microsoft.EntityFrameworkCore;
using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Infrastructure;

public sealed class EfRetentionReleaseRepository : IRetentionReleaseRepository
{
    private readonly ConstructionDbContext _dbContext;

    public EfRetentionReleaseRepository(ConstructionDbContext dbContext) => _dbContext = dbContext;

    public Task<RetentionRelease?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.RetentionReleases.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RetentionRelease>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.RetentionReleases.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.RetentionReleases.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<RetentionRelease>> ListByCommercialDocumentAsync(
        CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default) =>
        await _dbContext.RetentionReleases.AsNoTracking()
            .Where(r => r.CommercialDocumentType == commercialDocumentType && r.CommercialDocumentId == commercialDocumentId)
            .ToListAsync(cancellationToken);

    public void Add(RetentionRelease retentionRelease) => _dbContext.RetentionReleases.Add(retentionRelease);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
