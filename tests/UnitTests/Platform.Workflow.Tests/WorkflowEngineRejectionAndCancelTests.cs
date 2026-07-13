using Platform.Security;
using Platform.Workflow.Delegation;
using Platform.Workflow.Events;

namespace Platform.Workflow.Tests;

public class WorkflowEngineRejectionAndCancelTests
{
    private static SecurityPrincipal PrincipalWithRole(string userId, string roleKey) =>
        new(userId, new[] { roleKey }, new Dictionary<string, IReadOnlySet<string>>());

    private static WorkflowEngine BuildTwoStepEngine()
    {
        var definition = new WorkflowDefinition("PO_Approval", "PurchaseOrder", "Submit", new[]
        {
            new WorkflowStepDefinition("ManagerApproval", "Manager"),
            new WorkflowStepDefinition("FinanceApproval", "FinanceApprover")
        });
        var catalog = new InMemoryWorkflowDefinitionCatalog(new[] { definition });
        return new WorkflowEngine(catalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));
    }

    [Fact]
    public void Rejecting_the_first_step_short_circuits_the_whole_workflow()
    {
        var engine = BuildTwoStepEngine();
        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        engine.Decide(instance!, PrincipalWithRole("u.manager", "Manager"), WorkflowDecision.Reject, "Budget not approved.");

        Assert.Equal(WorkflowInstanceStatus.Rejected, instance!.Status);
        Assert.Single(instance.History);
        var completedEvent = Assert.Single(instance.DomainEvents.OfType<WorkflowCompletedEvent>());
        Assert.Equal(WorkflowInstanceStatus.Rejected, completedEvent.FinalStatus);
        Assert.Equal("Budget not approved.", completedEvent.Reason);
    }

    [Fact]
    public void Cannot_decide_on_an_already_finalized_instance()
    {
        var engine = BuildTwoStepEngine();
        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());
        engine.Decide(instance!, PrincipalWithRole("u.manager", "Manager"), WorkflowDecision.Reject);

        Assert.Throws<InvalidOperationException>(() =>
            engine.Decide(instance!, PrincipalWithRole("u.finance", "FinanceApprover"), WorkflowDecision.Approve));
    }

    [Fact]
    public void Cancel_finalizes_a_running_instance_without_requiring_a_decision()
    {
        var engine = BuildTwoStepEngine();
        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        engine.Cancel(instance!, PrincipalWithRole("u.requester", "Requester"), "Purchase no longer needed.");

        Assert.Equal(WorkflowInstanceStatus.Cancelled, instance!.Status);
        Assert.Null(instance.CurrentStepStartedAt);
    }
}
