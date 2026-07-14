using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Tests;

internal sealed class FakeCostCenterRepository : ICostCenterRepository
{
    private readonly Dictionary<Guid, CostCenter> _costCenters = new();

    public Task<CostCenter?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_costCenters.GetValueOrDefault(id));

    public Task<CostCenter?> GetByCodeAsync(string costCenterCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_costCenters.Values.FirstOrDefault(c => c.CostCenterCode == costCenterCode));

    public Task<IReadOnlyList<CostCenter>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CostCenter>>(
            _costCenters.Values.OrderBy(c => c.CostCenterCode).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_costCenters.Count);

    public void Add(CostCenter costCenter) => _costCenters[costCenter.Id] = costCenter;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
