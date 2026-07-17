namespace Platform.Workflow.Escalation;

/// <summary>
/// Pure query over a set of instances — finds ones whose current step has run longer than its
/// configured <see cref="WorkflowStepDefinition.ServiceLevelHours"/> (docs/architecture/04-platform-services.md
/// #4, "SLA timers with escalation"). Deliberately just a query, not a scheduler: the actual "run this
/// every N minutes and notify someone" job is a hosted background service (Gateway.Api concern), not
/// built yet — this is the logic that job will call, kept independently testable in the meantime.
/// </summary>
public static class EscalationScan
{
    public static IReadOnlyList<EscalationCandidate> FindOverdueInstances(IEnumerable<WorkflowInstance> instances, DateTimeOffset now)
    {
        var results = new List<EscalationCandidate>();

        foreach (var instance in instances)
        {
            if (instance.Status != WorkflowInstanceStatus.Running)
            {
                continue;
            }

            var step = instance.CurrentStep;
            if (step?.ServiceLevelHours is null || step.EscalateToRoleKey is null)
            {
                continue;
            }

            if (instance.CurrentStepStartedAt is not { } startedAt)
            {
                continue;
            }

            var deadline = startedAt.AddHours(step.ServiceLevelHours.Value);
            if (now > deadline)
            {
                results.Add(new EscalationCandidate(instance, step, step.EscalateToRoleKey, now - deadline));
            }
        }

        return results;
    }
}
