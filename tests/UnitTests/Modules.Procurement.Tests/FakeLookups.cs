using Modules.MasterData.Contracts;

namespace Modules.Procurement.Tests;

/// <summary>In-memory stand-ins for the Modules.MasterData.Contracts lookups
/// PurchaseRequisitionService depends on — proves its own cross-module validation logic without a real
/// MasterData database. The real adapters (<c>Modules.MasterData.Infrastructure.EfItemLookup</c>/
/// <c>EfCostCenterLookup</c>) are proved separately by MasterData's own tests.</summary>
internal sealed class FakeItemLookup : IItemLookup
{
    private readonly Dictionary<Guid, ItemSummary> _items = new();

    public void Add(ItemSummary item) => _items[item.Id] = item;

    public Task<ItemSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));
}

internal sealed class FakeCostCenterLookup : ICostCenterLookup
{
    private readonly Dictionary<Guid, CostCenterSummary> _costCenters = new();

    public void Add(CostCenterSummary costCenter) => _costCenters[costCenter.Id] = costCenter;

    public Task<CostCenterSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_costCenters.GetValueOrDefault(id));
}
