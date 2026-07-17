using Modules.Construction.Domain;

namespace Modules.Construction.Application;

public interface IVariationOrderRepository
{
    Task<VariationOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VariationOrder>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(VariationOrder variationOrder);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
