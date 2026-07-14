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

    // .Include() is required here — EF Core does not eager-load navigation collections by default, so
    // Addresses/Contacts would silently come back empty without this even though the child rows exist.
    private IQueryable<BusinessPartner> WithChildren() =>
        _dbContext.BusinessPartners.Include(p => p.Addresses).Include(p => p.Contacts);

    public Task<BusinessPartner?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithChildren().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BusinessPartner>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await WithChildren()
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
