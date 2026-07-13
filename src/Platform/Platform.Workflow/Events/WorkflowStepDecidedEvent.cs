using Platform.Core.Events;

namespace Platform.Workflow.Events;

/// <summary>Raised every time a step is decided — this is what lets Audit and (once Platform.Events'
/// bus exists) notification listeners react without the workflow engine knowing they exist.</summary>
public sealed record WorkflowStepDecidedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid WorkflowInstanceId,
    string StepId,
    string ActorUserId,
    WorkflowDecision Decision) : IDomainEvent
{
    public static WorkflowStepDecidedEvent Create(Guid workflowInstanceId, string stepId, string actorUserId, WorkflowDecision decision)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, workflowInstanceId, stepId, actorUserId, decision);
}
