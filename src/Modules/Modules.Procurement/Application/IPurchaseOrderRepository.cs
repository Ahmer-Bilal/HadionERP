using Modules.Procurement.Domain;

namespace Modules.Procurement.Application;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrder>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(PurchaseOrder purchaseOrder);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
