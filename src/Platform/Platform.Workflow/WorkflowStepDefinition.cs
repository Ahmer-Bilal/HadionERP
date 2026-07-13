namespace Platform.Workflow;

/// <summary>
/// One level of an approval matrix (docs/architecture/03-platform-services.md #4). A step only applies
/// to a given run if <see cref="Condition"/> is satisfied by the resource context at start time (e.g. a
/// "second approver" step with Condition {"MinAmount": "50000"} only applies to POs over that amount) —
/// this is what makes the matrix configurable per amount/cost-center/project rather than fixed code.
/// </summary>
public sealed record WorkflowStepDefinition(
    string StepId,
    string RequiredRoleKey,
    ApprovalQuorum Quorum = ApprovalQuorum.Any,
    IReadOnlyDictionary<string, string>? Condition = null,
    int? ServiceLevelHours = null,
    string? EscalateToRoleKey = null);
