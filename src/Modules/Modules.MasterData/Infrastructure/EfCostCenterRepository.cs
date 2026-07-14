using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Infrastructure;

public sealed class EfCostCenterRepository : ICostCenterRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfCostCenterRepository(MasterDataDbContext dbContext) => _dbContext = dbContext;

    // Tracked (not AsNoTracking): the Update/Submit/Approve paths load via GetAsync then mutate + Save,
    // so EF must observe the entity for changes. GetByCode (a read-only uniqueness check) and List stay
    // AsNoTracking — same rationale as EfGLAccountRepository/EfItemRepository.
    public Task<CostCenter?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CostCenters.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CostCenter?> GetByCodeAsync(string costCenterCode, CancellationToken cancellationToken = default) =>
        _dbContext.CostCenters.AsNoTracking().FirstOrDefaultAsync(c => c.CostCenterCode == costCenterCode, cancellationToken);

    public async Task<IReadOnlyList<CostCenter>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.CostCenters.AsNoTracking().OrderBy(c => c.CostCenterCode).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.CostCenters.CountAsync(cancellationToken);

    public void Add(CostCenter costCenter) => _dbContext.CostCenters.Add(costCenter);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
