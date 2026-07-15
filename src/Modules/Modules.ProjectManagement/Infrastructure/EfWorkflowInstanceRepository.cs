using Microsoft.EntityFrameworkCore;
using Platform.Workflow;

namespace Modules.ProjectManagement.Infrastructure;

/// <summary>Implements <see cref="IWorkflowInstanceRepository"/> against this module's own
/// <see cref="ProjectManagementDbContext"/> — a near-duplicate of every other module's own copy, for the
/// same "each module owns its own schema" reason as <see cref="EfCoreNumberRangeService"/>.</summary>
public sealed class EfWorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly ProjectManagementDbContext _dbContext;

    public EfWorkflowInstanceRepository(ProjectManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(WorkflowInstance instance) => _dbContext.WorkflowInstances.Add(instance);

    public Task<WorkflowInstance?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowInstances.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<WorkflowInstance?> GetActiveAsync(string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowInstances.FirstOrDefaultAsync(
            i => i.BusinessObjectType == businessObjectType
                && i.BusinessObjectId == businessObjectId
                && i.Status == WorkflowInstanceStatus.Running,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
