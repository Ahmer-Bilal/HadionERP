using Modules.MasterData.Domain;

namespace Modules.MasterData.Application;

/// <summary>The persistence port for Tax Codes — same dependency-inversion shape as
/// <see cref="IItemRepository"/>, including <see cref="GetByCodeAsync"/> for the unique-tax-code check.</summary>
public interface ITaxCodeRepository
{
    Task<TaxCode?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaxCode?> GetByCodeAsync(string taxCodeCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxCode>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(TaxCode taxCode);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
