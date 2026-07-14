using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Infrastructure;

public sealed class EfTaxCodeRepository : ITaxCodeRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfTaxCodeRepository(MasterDataDbContext dbContext) => _dbContext = dbContext;

    public Task<TaxCode?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.TaxCodes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TaxCode?> GetByCodeAsync(string taxCodeCode, CancellationToken cancellationToken = default) =>
        _dbContext.TaxCodes.AsNoTracking().FirstOrDefaultAsync(t => t.TaxCodeCode == taxCodeCode, cancellationToken);

    public async Task<IReadOnlyList<TaxCode>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.TaxCodes.AsNoTracking().OrderBy(t => t.TaxCodeCode).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.TaxCodes.CountAsync(cancellationToken);

    public void Add(TaxCode taxCode) => _dbContext.TaxCodes.Add(taxCode);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
