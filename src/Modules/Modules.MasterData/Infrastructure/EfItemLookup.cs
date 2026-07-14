using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Contracts;

namespace Modules.MasterData.Infrastructure;

/// <summary>Implements the published <see cref="IItemLookup"/> contract — same "project straight off the
/// module's own DbContext" pattern as <see cref="EfGLAccountLookup"/>.</summary>
public sealed class EfItemLookup : IItemLookup
{
    private readonly MasterDataDbContext _dbContext;

    public EfItemLookup(MasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<ItemSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return item is null
            ? null
            : new ItemSummary(item.Id, item.ItemCode, item.ItemName, item.UnitOfMeasure, item.IsActive);
    }
}
