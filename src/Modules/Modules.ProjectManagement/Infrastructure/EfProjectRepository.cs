using Microsoft.EntityFrameworkCore;
using Modules.ProjectManagement.Application;
using Modules.ProjectManagement.Domain;

namespace Modules.ProjectManagement.Infrastructure;

public sealed class EfProjectRepository : IProjectRepository
{
    private readonly ProjectManagementDbContext _dbContext;

    public EfProjectRepository(ProjectManagementDbContext dbContext) => _dbContext = dbContext;

    public Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Projects.Include(p => p.WbsElements).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        await _dbContext.Projects.AsNoTracking().Include(p => p.WbsElements)
            .OrderByDescending(p => p.CreatedAt).Skip(skip).Take(top).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Projects.CountAsync(cancellationToken);

    public void Add(Project project) => _dbContext.Projects.Add(project);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
