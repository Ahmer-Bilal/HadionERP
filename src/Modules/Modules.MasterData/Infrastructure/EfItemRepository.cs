using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Infrastructure;

public sealed class EfItemRepository : IItemRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfItemRepository(MasterDataDbContext dbContext) => _dbContext = dbContext;

    // Tracked (not AsNoTracking): the Update/Submit/Approve paths load via GetAsync then mutate + Save,
    // so EF must observe the entity for changes. GetByCode (a read-only uniqueness check) and List stay
    // AsNoTracking — same rationale as EfGLAccountRepository.
    public Task<Item?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Item?> GetByCodeAsync(string itemCode, CancellationToken cancellationToken = default) =>
        _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemCode == itemCode, cancellationToken);

    public async Task<IReadOnlyList<Item>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.Items.AsNoTracking().OrderBy(i => i.ItemCode).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Items.CountAsync(cancellationToken);

    public void Add(Item item) => _dbContext.Items.Add(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
