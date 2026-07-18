using Modules.Identity.Contracts;

namespace Modules.Finance.Tests;

internal sealed class FakeUserLookup : IUserLookup
{
    private readonly Dictionary<Guid, UserSummary> _users = new();

    public void Add(UserSummary user) => _users[user.Id] = user;

    public Task<UserSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.GetValueOrDefault(id));

    public Task<IReadOnlyList<UserSummary>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserSummary>>(_users.Values.Where(u => u.IsActive).ToList());
}
