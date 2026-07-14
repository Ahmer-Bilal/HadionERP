using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Tests;

internal sealed class FakeVendorPrequalificationRepository : IVendorPrequalificationRepository
{
    private readonly Dictionary<Guid, VendorPrequalification> _items = new();

    public Task<VendorPrequalification?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<VendorPrequalification>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<VendorPrequalification>>(
            _items.Values.OrderByDescending(p => p.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(VendorPrequalification prequalification) => _items[prequalification.Id] = prequalification;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
