using Platform.Security;

namespace Platform.Workflow;

/// <summary>Decides whether a principal may act on a given step — either by directly holding the
/// step's required Role, or through an active delegation.</summary>
public interface IWorkflowEligibilityService
{
    bool CanAct(SecurityPrincipal principal, WorkflowStepDefinition step, DateOnly onDate);
}
