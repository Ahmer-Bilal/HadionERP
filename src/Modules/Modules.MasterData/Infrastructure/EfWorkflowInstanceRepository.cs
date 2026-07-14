using Microsoft.EntityFrameworkCore;
using Platform.Workflow;

namespace Modules.MasterData.Infrastructure;

public sealed class EfWorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfWorkflowInstanceRepository(MasterDataDbContext dbContext)
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
