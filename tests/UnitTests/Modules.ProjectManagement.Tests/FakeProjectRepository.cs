using Modules.ProjectManagement.Application;
using Modules.ProjectManagement.Domain;

namespace Modules.ProjectManagement.Tests;

internal sealed class FakeProjectRepository : IProjectRepository
{
    private readonly Dictionary<Guid, Project> _items = new();

    public Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<Project>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Project>>(
            _items.Values.OrderByDescending(p => p.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(Project project) => _items[project.Id] = project;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
