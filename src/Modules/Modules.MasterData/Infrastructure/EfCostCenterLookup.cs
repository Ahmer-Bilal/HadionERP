using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Contracts;

namespace Modules.MasterData.Infrastructure;

/// <summary>Implements the published <see cref="ICostCenterLookup"/> contract — same
/// "project straight off the module's own DbContext" pattern as <see cref="EfGLAccountLookup"/>.</summary>
public sealed class EfCostCenterLookup : ICostCenterLookup
{
    private readonly MasterDataDbContext _dbContext;

    public EfCostCenterLookup(MasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<CostCenterSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var costCenter = await _dbContext.CostCenters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return costCenter is null
            ? null
            : new CostCenterSummary(costCenter.Id, costCenter.CostCenterCode, costCenter.CostCenterName, costCenter.IsPostable, costCenter.IsActive);
    }
}
