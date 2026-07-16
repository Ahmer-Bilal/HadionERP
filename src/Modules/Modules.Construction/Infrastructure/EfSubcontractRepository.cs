using Microsoft.EntityFrameworkCore;
using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Infrastructure;

public sealed class EfSubcontractRepository : ISubcontractRepository
{
    private readonly ConstructionDbContext _dbContext;

    public EfSubcontractRepository(ConstructionDbContext dbContext) => _dbContext = dbContext;

    public Task<Subcontract?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Subcontracts.Include(s => s.Lines).Include(s => s.BackCharges)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Subcontract>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.Subcontracts.AsNoTracking().Include(s => s.Lines).Include(s => s.BackCharges)
            .OrderByDescending(s => s.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Subcontracts.CountAsync(cancellationToken);

    public void Add(Subcontract subcontract) => _dbContext.Subcontracts.Add(subcontract);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
