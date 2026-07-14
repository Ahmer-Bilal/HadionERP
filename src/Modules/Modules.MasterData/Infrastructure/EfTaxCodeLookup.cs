using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Contracts;

namespace Modules.MasterData.Infrastructure;

/// <summary>Implements the published <see cref="ITaxCodeLookup"/> contract — same
/// "project straight off the module's own DbContext" pattern as <see cref="EfGLAccountLookup"/>.</summary>
public sealed class EfTaxCodeLookup : ITaxCodeLookup
{
    private readonly MasterDataDbContext _dbContext;

    public EfTaxCodeLookup(MasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<TaxCodeSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var taxCode = await _dbContext.TaxCodes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return taxCode is null
            ? null
            : new TaxCodeSummary(taxCode.Id, taxCode.TaxCodeCode, taxCode.TaxCodeName, taxCode.Rate, taxCode.TaxType.ToString(), taxCode.IsActive);
    }
}
