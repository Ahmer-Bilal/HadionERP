using Modules.MasterData.Contracts;

namespace Modules.Finance.Tests;

/// <summary>In-memory stand-ins for the Modules.MasterData.Contracts lookups Modules.Finance depends on —
/// proves JournalEntryService's own cross-module validation logic without a real MasterData database. The
/// real adapters (<c>Modules.MasterData.Infrastructure.EfGLAccountLookup</c>/<c>EfCostCenterLookup</c>) are
/// proved separately by MasterData's own tests and by this slice's integration test.</summary>
internal sealed class FakeGLAccountLookup : IGLAccountLookup
{
    private readonly Dictionary<Guid, GLAccountSummary> _accounts = new();

    public void Add(GLAccountSummary account) => _accounts[account.Id] = account;

    public Task<GLAccountSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.GetValueOrDefault(id));
}

internal sealed class FakeCostCenterLookup : ICostCenterLookup
{
    private readonly Dictionary<Guid, CostCenterSummary> _costCenters = new();

    public void Add(CostCenterSummary costCenter) => _costCenters[costCenter.Id] = costCenter;

    public Task<CostCenterSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_costCenters.GetValueOrDefault(id));
}
