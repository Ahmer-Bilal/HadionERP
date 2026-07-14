using Modules.MasterData.Domain;

namespace Modules.MasterData.Application;

/// <summary>The persistence port for G/L Accounts — same dependency-inversion shape as
/// <see cref="IBusinessPartnerRepository"/>. Adds <see cref="GetByCodeAsync"/> for the unique-account-code
/// check Business Partner didn't need (BP names aren't unique; account codes are).</summary>
public interface IGLAccountRepository
{
    Task<GLAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GLAccount?> GetByCodeAsync(string accountCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GLAccount>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(GLAccount account);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
