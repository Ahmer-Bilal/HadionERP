namespace Platform.Workflow;

/// <summary>One recorded decision against one step — the raw material of the "Approval History" facet
/// on a Business Object's record form (docs/architecture/02-business-object-model.md #2.1).</summary>
public sealed record WorkflowStepDecisionRecord(
    string StepId,
    string ActorUserId,
    WorkflowDecision Decision,
    string? Comment,
    DateTimeOffset DecidedAt);
