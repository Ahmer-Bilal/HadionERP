using Modules.MasterData.Domain;

namespace Modules.MasterData.Application;

/// <summary>The persistence port for Items — same dependency-inversion shape as
/// <see cref="IGLAccountRepository"/>, including <see cref="GetByCodeAsync"/> for the unique-item-code
/// check.</summary>
public interface IItemRepository
{
    Task<Item?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Item?> GetByCodeAsync(string itemCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(Item item);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
