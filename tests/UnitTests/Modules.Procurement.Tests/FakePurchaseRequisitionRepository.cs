using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Tests;

internal sealed class FakePurchaseRequisitionRepository : IPurchaseRequisitionRepository
{
    private readonly Dictionary<Guid, PurchaseRequisition> _items = new();

    public Task<PurchaseRequisition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<PurchaseRequisition>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PurchaseRequisition>>(
            _items.Values.OrderByDescending(r => r.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(PurchaseRequisition requisition) => _items[requisition.Id] = requisition;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
