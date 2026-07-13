namespace Platform.Workflow.Escalation;

/// <summary>One running instance whose current step has exceeded its configured SLA.</summary>
public sealed record EscalationCandidate(WorkflowInstance Instance, WorkflowStepDefinition Step, string EscalateToRoleKey, TimeSpan Overdue);
