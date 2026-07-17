using Microsoft.EntityFrameworkCore;
using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Infrastructure;

public sealed class EfMeasurementSheetRepository : IMeasurementSheetRepository
{
    private readonly ConstructionDbContext _dbContext;

    public EfMeasurementSheetRepository(ConstructionDbContext dbContext) => _dbContext = dbContext;

    public Task<MeasurementSheet?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.MeasurementSheets.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MeasurementSheet>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.MeasurementSheets.AsNoTracking().Include(s => s.Lines)
            .OrderByDescending(s => s.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.MeasurementSheets.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<MeasurementSheet>> ListByCommercialDocumentAsync(
        CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default) =>
        await _dbContext.MeasurementSheets.AsNoTracking().Include(s => s.Lines)
            .Where(s => s.CommercialDocumentType == commercialDocumentType && s.CommercialDocumentId == commercialDocumentId)
            .ToListAsync(cancellationToken);

    public void Add(MeasurementSheet measurementSheet) => _dbContext.MeasurementSheets.Add(measurementSheet);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
