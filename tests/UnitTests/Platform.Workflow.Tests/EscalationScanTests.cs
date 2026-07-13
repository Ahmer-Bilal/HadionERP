using Platform.Security;
using Platform.Workflow.Delegation;
using Platform.Workflow.Escalation;

namespace Platform.Workflow.Tests;

public class EscalationScanTests
{
    private static WorkflowEngine BuildEngine(WorkflowStepDefinition step)
    {
        var definition = new WorkflowDefinition("PO_Approval", "PurchaseOrder", "Submit", new[] { step });
        var catalog = new InMemoryWorkflowDefinitionCatalog(new[] { definition });
        return new WorkflowEngine(catalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));
    }

    [Fact]
    public void A_step_past_its_SLA_is_flagged_for_escalation()
    {
        var engine = BuildEngine(new WorkflowStepDefinition(
            "ManagerApproval", "Manager", ServiceLevelHours: 0, EscalateToRoleKey: "SeniorManager"));

        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());
        var wellPastDeadline = DateTimeOffset.UtcNow.AddHours(1);

        var overdue = EscalationScan.FindOverdueInstances(new[] { instance! }, wellPastDeadline);

        var candidate = Assert.Single(overdue);
        Assert.Equal("SeniorManager", candidate.EscalateToRoleKey);
        Assert.Equal("ManagerApproval", candidate.Step.StepId);
        Assert.True(candidate.Overdue > TimeSpan.Zero);
    }

    [Fact]
    public void A_step_still_within_its_SLA_is_not_flagged()
    {
        var engine = BuildEngine(new WorkflowStepDefinition(
            "ManagerApproval", "Manager", ServiceLevelHours: 100, EscalateToRoleKey: "SeniorManager"));

        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        var overdue = EscalationScan.FindOverdueInstances(new[] { instance! }, DateTimeOffset.UtcNow);

        Assert.Empty(overdue);
    }

    [Fact]
    public void Steps_with_no_configured_SLA_are_never_flagged()
    {
        var engine = BuildEngine(new WorkflowStepDefinition("ManagerApproval", "Manager"));
        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        var overdue = EscalationScan.FindOverdueInstances(new[] { instance! }, DateTimeOffset.UtcNow.AddYears(1));

        Assert.Empty(overdue);
    }

    [Fact]
    public void A_completed_instance_is_never_flagged_even_if_it_would_otherwise_be_overdue()
    {
        var engine = BuildEngine(new WorkflowStepDefinition(
            "ManagerApproval", "Manager", ServiceLevelHours: 0, EscalateToRoleKey: "SeniorManager"));

        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());
        engine.Cancel(instance!, new SecurityPrincipal("u.requester", Array.Empty<string>(), new Dictionary<string, IReadOnlySet<string>>()), "no longer needed");

        var overdue = EscalationScan.FindOverdueInstances(new[] { instance! }, DateTimeOffset.UtcNow.AddHours(1));

        Assert.Empty(overdue);
    }
}
