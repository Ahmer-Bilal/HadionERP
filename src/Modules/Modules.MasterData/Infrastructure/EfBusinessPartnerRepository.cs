using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Infrastructure;

public sealed class EfBusinessPartnerRepository : IBusinessPartnerRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfBusinessPartnerRepository(MasterDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<BusinessPartner?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.BusinessPartners.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BusinessPartner>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.BusinessPartners
            .OrderBy(p => p.CreatedAt)
            .Skip(skip)
            .Take(top)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.BusinessPartners.CountAsync(cancellationToken);

    public void Add(BusinessPartner partner) => _dbContext.BusinessPartners.Add(partner);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
