using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

internal sealed class FakeClosingActivityRepository : IClosingActivityRepository
{
    private readonly Dictionary<Guid, ClosingActivity> _activities = new();

    public Task<ClosingActivity?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_activities.GetValueOrDefault(id));

    public Task<IReadOnlyList<ClosingActivity>> ListForPeriodAsync(Guid fiscalPeriodId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClosingActivity>>(
            _activities.Values.Where(a => a.FiscalPeriodId == fiscalPeriodId).OrderBy(a => a.SequenceNumber).ToList());

    public void Add(ClosingActivity activity) => _activities[activity.Id] = activity;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
