using Modules.Procurement.Domain;

namespace Modules.Procurement.Application;

public interface IPurchaseRequisitionRepository
{
    Task<PurchaseRequisition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseRequisition>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(PurchaseRequisition requisition);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
