using Platform.Security;

namespace Platform.Workflow.Tests;

public class WorkflowEngineSequentialApprovalTests
{
    private static WorkflowEngine BuildEngine(params WorkflowStepDefinition[] steps)
    {
        var definition = new WorkflowDefinition("PO_Approval", "PurchaseOrder", "Submit", steps);
        var catalog = new InMemoryWorkflowDefinitionCatalog(new[] { definition });
        var eligibility = new RoleBasedWorkflowEligibilityService(new Delegation.InMemoryDelegationRegistry());
        return new WorkflowEngine(catalog, eligibility);
    }

    private static SecurityPrincipal PrincipalWithRole(string userId, string roleKey) =>
        new(userId, new[] { roleKey }, new Dictionary<string, IReadOnlySet<string>>());

    [Fact]
    public void No_workflow_configured_means_Start_returns_null()
    {
        var catalog = new InMemoryWorkflowDefinitionCatalog(Array.Empty<WorkflowDefinition>());
        var engine = new WorkflowEngine(catalog, new RoleBasedWorkflowEligibilityService(new Delegation.InMemoryDelegationRegistry()));

        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        Assert.Null(instance);
    }

    [Fact]
    public void Zero_applicable_steps_auto_approves_immediately()
    {
        var engine = BuildEngine(new WorkflowStepDefinition(
            "FinanceApproval", "FinanceApprover",
            Condition: new Dictionary<string, string> { ["MinAmount"] = "50000" }));

        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid(),
            resourceContext: new Dictionary<string, string> { ["Amount"] = "1000" });

        Assert.NotNull(instance);
        Assert.Equal(WorkflowInstanceStatus.Approved, instance!.Status);
        Assert.Empty(instance.ApplicableSteps);
        Assert.Contains(instance.DomainEvents, e => e is Events.WorkflowCompletedEvent);
    }

    [Fact]
    public void Only_the_second_step_applies_when_amount_exceeds_its_threshold()
    {
        var engine = BuildEngine(
            new WorkflowStepDefinition("ManagerApproval", "Manager"),
            new WorkflowStepDefinition("FinanceApproval", "FinanceApprover",
                Condition: new Dictionary<string, string> { ["MinAmount"] = "50000" }));

        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid(),
            resourceContext: new Dictionary<string, string> { ["Amount"] = "10000" });

        Assert.NotNull(instance);
        var stepIds = instance!.ApplicableSteps.Select(s => s.StepId).ToList();
        Assert.Equal(new[] { "ManagerApproval" }, stepIds);
    }

    [Fact]
    public void Both_steps_apply_and_must_be_approved_in_order()
    {
        var engine = BuildEngine(
            new WorkflowStepDefinition("ManagerApproval", "Manager"),
            new WorkflowStepDefinition("FinanceApproval", "FinanceApprover",
                Condition: new Dictionary<string, string> { ["MinAmount"] = "50000" }));

        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid(),
            resourceContext: new Dictionary<string, string> { ["Amount"] = "75000" });

        Assert.NotNull(instance);
        Assert.Equal(WorkflowInstanceStatus.Running, instance!.Status);
        Assert.Equal("ManagerApproval", instance.CurrentStep!.StepId);

        engine.Decide(instance, PrincipalWithRole("u.manager", "Manager"), WorkflowDecision.Approve);
        Assert.Equal(WorkflowInstanceStatus.Running, instance.Status);
        Assert.Equal("FinanceApproval", instance.CurrentStep!.StepId);

        engine.Decide(instance, PrincipalWithRole("u.finance", "FinanceApprover"), WorkflowDecision.Approve);
        Assert.Equal(WorkflowInstanceStatus.Approved, instance.Status);
        Assert.Equal(2, instance.History.Count);
    }
}
