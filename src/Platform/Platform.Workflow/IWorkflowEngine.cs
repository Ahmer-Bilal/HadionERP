using Platform.Security;

namespace Platform.Workflow;

/// <summary>
/// The single entry point modules use to run approval workflows — mirrors how
/// Platform.Security.IAuthorizationService is the single entry point for permission checks. A module's
/// Application layer calls Start() when a BO transition that might need approval happens, and listens for
/// Platform.Workflow.Events.WorkflowCompletedEvent to drive the BO's own subsequent transition.
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Starts a new instance if a workflow is configured for this Business Object type + transition;
    /// returns null if none is configured, meaning no approval workflow applies at all and the caller
    /// should proceed as if already approved.
    /// </summary>
    WorkflowInstance? Start(
        string businessObjectType,
        string triggerTransition,
        Guid businessObjectId,
        IReadOnlyDictionary<string, string>? resourceContext = null,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? requiredApproversByStep = null);

    /// <summary>Records <paramref name="actor"/>'s decision against the instance's current step, after
    /// verifying they're eligible to act on it. Throws <see cref="UnauthorizedAccessException"/> if not.</summary>
    void Decide(WorkflowInstance instance, SecurityPrincipal actor, WorkflowDecision decision, string? comment = null, DateOnly? onDate = null);

    void Cancel(WorkflowInstance instance, SecurityPrincipal actor, string reason);
}
