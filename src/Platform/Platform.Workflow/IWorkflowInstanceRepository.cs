namespace Platform.Workflow;

/// <summary>
/// The persistence port for <see cref="WorkflowInstance"/> — mirrors
/// <c>Platform.Core.NumberRanges.INumberRangeService</c>'s pattern (a storage-agnostic kernel interface,
/// implemented per-module against that module's own real database, e.g.
/// <c>Modules.MasterData.Infrastructure.EfWorkflowInstanceRepository</c>). Platform.Workflow itself has no
/// database dependency; an approval can span multiple separate HTTP requests (a submit today, a decision
/// days later), so a running instance's state must survive between them somewhere real.
/// </summary>
public interface IWorkflowInstanceRepository
{
    void Add(WorkflowInstance instance);

    Task<WorkflowInstance?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The currently-running instance for a Business Object, or null if none is running (either
    /// nothing was ever started, or the last one already reached a terminal status). Assumes at most one
    /// Running instance exists per Business Object at a time — true today for every module's usage (a
    /// document can only be submitted once before its workflow resolves); revisit if a module ever allows
    /// starting a second approval while one is still in flight.</summary>
    Task<WorkflowInstance?> GetActiveAsync(string businessObjectType, Guid businessObjectId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
