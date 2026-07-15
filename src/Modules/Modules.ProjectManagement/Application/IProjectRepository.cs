using Modules.ProjectManagement.Domain;

namespace Modules.ProjectManagement.Application;

public interface IProjectRepository
{
    Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(Project project);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
