using Microsoft.EntityFrameworkCore;
using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Infrastructure;

public sealed class EfIpcRepository : IIpcRepository
{
    private readonly ConstructionDbContext _dbContext;

    public EfIpcRepository(ConstructionDbContext dbContext) => _dbContext = dbContext;

    public Task<Ipc?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Ipcs.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Ipc>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.Ipcs.AsNoTracking().Include(i => i.Lines)
            .OrderByDescending(i => i.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Ipcs.CountAsync(cancellationToken);

    public Task<bool> ExistsForMeasurementSheetAsync(Guid measurementSheetId, CancellationToken cancellationToken = default) =>
        _dbContext.Ipcs.AnyAsync(i => i.MeasurementSheetId == measurementSheetId, cancellationToken);

    public void Add(Ipc ipc) => _dbContext.Ipcs.Add(ipc);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
