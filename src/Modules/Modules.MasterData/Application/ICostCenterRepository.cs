using Modules.MasterData.Domain;

namespace Modules.MasterData.Application;

/// <summary>The persistence port for Cost Centers — same dependency-inversion shape as
/// <see cref="IGLAccountRepository"/>, including <see cref="GetByCodeAsync"/> for the unique-cost-center-
/// code check.</summary>
public interface ICostCenterRepository
{
    Task<CostCenter?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CostCenter?> GetByCodeAsync(string costCenterCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CostCenter>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(CostCenter costCenter);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
