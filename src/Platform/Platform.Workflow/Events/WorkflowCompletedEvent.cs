using Platform.Core.Events;

namespace Platform.Workflow.Events;

/// <summary>Raised once, when a workflow instance reaches a final status (Approved/Rejected/Cancelled).
/// The Application layer that started the workflow listens for this to drive the underlying Business
/// Object's own transition (e.g. call bo.Transition(Approve, ...) — the workflow engine itself never
/// touches a Business Object directly, keeping Platform.Workflow decoupled from every module's domain
/// types, per docs/architecture/01-overview.md #1).</summary>
public sealed record WorkflowCompletedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid WorkflowInstanceId,
    Guid BusinessObjectId,
    string BusinessObjectType,
    WorkflowInstanceStatus FinalStatus,
    string? Reason) : IDomainEvent
{
    public static WorkflowCompletedEvent Create(
        Guid workflowInstanceId,
        Guid businessObjectId,
        string businessObjectType,
        WorkflowInstanceStatus finalStatus,
        string? reason = null)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, workflowInstanceId, businessObjectId, businessObjectType, finalStatus, reason);
}
