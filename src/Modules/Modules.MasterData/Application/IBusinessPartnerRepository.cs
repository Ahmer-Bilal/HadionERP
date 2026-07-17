using Modules.MasterData.Domain;

namespace Modules.MasterData.Application;

/// <summary>The persistence port this Application layer depends on — Infrastructure provides the real
/// (EF Core) implementation, per docs/architecture/01-overview.md #1's dependency
/// inversion rule (Application never references Infrastructure directly).</summary>
public interface IBusinessPartnerRepository
{
    Task<BusinessPartner?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessPartner>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(BusinessPartner partner);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
