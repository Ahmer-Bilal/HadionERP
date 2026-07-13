using Platform.Security;
using Platform.Workflow.Delegation;

namespace Platform.Workflow;

/// <summary>
/// A principal may act on a step if they directly hold the step's <see cref="WorkflowStepDefinition.RequiredRoleKey"/>
/// (via Platform.Security), or if someone has delegated that role to them for the given date (an
/// out-of-office cover).
/// </summary>
public sealed class RoleBasedWorkflowEligibilityService : IWorkflowEligibilityService
{
    private readonly IDelegationRegistry _delegationRegistry;

    public RoleBasedWorkflowEligibilityService(IDelegationRegistry delegationRegistry)
    {
        _delegationRegistry = delegationRegistry;
    }

    public bool CanAct(SecurityPrincipal principal, WorkflowStepDefinition step, DateOnly onDate) =>
        principal.RoleKeys.Contains(step.RequiredRoleKey)
        || _delegationRegistry.HasActiveDelegation(principal.UserId, step.RequiredRoleKey, onDate);
}
