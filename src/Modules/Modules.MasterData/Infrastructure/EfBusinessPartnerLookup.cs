using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Contracts;

namespace Modules.MasterData.Infrastructure;

/// <summary>Implements the published <see cref="IBusinessPartnerLookup"/> contract — same
/// "project straight off the module's own DbContext" pattern as <see cref="EfGLAccountLookup"/>.</summary>
public sealed class EfBusinessPartnerLookup : IBusinessPartnerLookup
{
    private readonly MasterDataDbContext _dbContext;

    public EfBusinessPartnerLookup(MasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<BusinessPartnerSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var partner = await _dbContext.BusinessPartners.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return partner is null
            ? null
            : new BusinessPartnerSummary(partner.Id, partner.Name, partner.NameArabic, partner.PartnerType.ToString(), partner.Status.ToString());
    }
}
