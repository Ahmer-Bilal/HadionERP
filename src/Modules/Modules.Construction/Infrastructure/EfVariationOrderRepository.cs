using Microsoft.EntityFrameworkCore;
using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Infrastructure;

public sealed class EfVariationOrderRepository : IVariationOrderRepository
{
    private readonly ConstructionDbContext _dbContext;

    public EfVariationOrderRepository(ConstructionDbContext dbContext) => _dbContext = dbContext;

    public Task<VariationOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.VariationOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VariationOrder>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.VariationOrders.AsNoTracking().Include(o => o.Lines)
            .OrderByDescending(o => o.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.VariationOrders.CountAsync(cancellationToken);

    public void Add(VariationOrder variationOrder) => _dbContext.VariationOrders.Add(variationOrder);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
