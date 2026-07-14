using Modules.Procurement.Domain;

namespace Modules.Procurement.Application;

public interface IVendorPrequalificationRepository
{
    Task<VendorPrequalification?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorPrequalification>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(VendorPrequalification prequalification);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
