using Microsoft.EntityFrameworkCore;
using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Infrastructure;

public sealed class EfContractRepository : IContractRepository
{
    private readonly ConstructionDbContext _dbContext;

    public EfContractRepository(ConstructionDbContext dbContext) => _dbContext = dbContext;

    public Task<Contract?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Contracts.Include(c => c.BoqLines).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Contract>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.Contracts.AsNoTracking().Include(c => c.BoqLines)
            .OrderByDescending(c => c.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Contracts.CountAsync(cancellationToken);

    public void Add(Contract contract) => _dbContext.Contracts.Add(contract);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
