using Platform.Workflow;

namespace Modules.ProjectManagement.Tests;

/// <summary>An in-memory stand-in for <see cref="IWorkflowInstanceRepository"/> — same role as every other
/// module's own copy of this fake. The real implementation
/// (<c>Modules.ProjectManagement.Infrastructure.EfWorkflowInstanceRepository</c>) is proved separately by an
/// integration test against real PostgreSQL.</summary>
internal sealed class FakeWorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly Dictionary<Guid, WorkflowInstance> _instances = new();

    public void Add(WorkflowInstance instance) => _instances[instance.Id] = instance;

    public Task<WorkflowInstance?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_instances.GetValueOrDefault(id));

    public Task<WorkflowInstance?> GetActiveAsync(string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_instances.Values.FirstOrDefault(i =>
            i.BusinessObjectType == businessObjectType && i.BusinessObjectId == businessObjectId && i.Status == WorkflowInstanceStatus.Running));

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
