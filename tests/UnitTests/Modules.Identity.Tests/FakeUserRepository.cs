using Modules.Identity.Application;
using Modules.Identity.Domain;

namespace Modules.Identity.Tests;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.GetValueOrDefault(id));

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.Username == username));

    public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(_users.Values.OrderBy(u => u.Username).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_users.Count);

    public void Add(User user) => _users[user.Id] = user;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
