using Modules.ProjectManagement.Contracts;

namespace Modules.Construction.Tests;

internal sealed class FakeProjectLookup : IProjectLookup
{
    private readonly Dictionary<Guid, ProjectSummary> _projects = new();

    public void Add(ProjectSummary project) => _projects[project.Id] = project;

    public Task<ProjectSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_projects.GetValueOrDefault(id));
}
