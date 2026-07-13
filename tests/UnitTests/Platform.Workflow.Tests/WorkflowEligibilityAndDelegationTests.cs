using Platform.Security;
using Platform.Workflow.Delegation;

namespace Platform.Workflow.Tests;

public class WorkflowEligibilityAndDelegationTests
{
    private static SecurityPrincipal PrincipalWithRole(string userId, string roleKey) =>
        new(userId, new[] { roleKey }, new Dictionary<string, IReadOnlySet<string>>());

    private static SecurityPrincipal PrincipalWithNoRoles(string userId) =>
        new(userId, Array.Empty<string>(), new Dictionary<string, IReadOnlySet<string>>());

    private static WorkflowEngine BuildEngine(IDelegationRegistry delegationRegistry)
    {
        var definition = new WorkflowDefinition("PO_Approval", "PurchaseOrder", "Submit", new[]
        {
            new WorkflowStepDefinition("ManagerApproval", "Manager")
        });
        var catalog = new InMemoryWorkflowDefinitionCatalog(new[] { definition });
        return new WorkflowEngine(catalog, new RoleBasedWorkflowEligibilityService(delegationRegistry));
    }

    [Fact]
    public void An_actor_without_the_required_role_or_a_delegation_is_rejected()
    {
        var engine = BuildEngine(new InMemoryDelegationRegistry());
        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        var ex = Assert.Throws<UnauthorizedAccessException>(() =>
            engine.Decide(instance!, PrincipalWithNoRoles("u.randomclerk"), WorkflowDecision.Approve));

        Assert.Contains("not eligible", ex.Message);
    }

    [Fact]
    public void A_delegate_covering_todays_date_can_act_even_without_holding_the_role_directly()
    {
        var registry = new InMemoryDelegationRegistry();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        registry.Register(new Delegation.Delegation("u.manager", "u.deputy", "Manager", today.AddDays(-1), today.AddDays(5)));

        var engine = BuildEngine(registry);
        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        engine.Decide(instance!, PrincipalWithNoRoles("u.deputy"), WorkflowDecision.Approve, onDate: today);

        Assert.Equal(WorkflowInstanceStatus.Approved, instance!.Status);
    }

    [Fact]
    public void A_delegation_outside_its_date_range_does_not_grant_eligibility()
    {
        var registry = new InMemoryDelegationRegistry();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        registry.Register(new Delegation.Delegation("u.manager", "u.deputy", "Manager", today.AddDays(10), today.AddDays(20)));

        var engine = BuildEngine(registry);
        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        Assert.Throws<UnauthorizedAccessException>(() =>
            engine.Decide(instance!, PrincipalWithNoRoles("u.deputy"), WorkflowDecision.Approve, onDate: today));
    }

    [Fact]
    public void Holding_the_role_directly_still_works_without_any_delegation()
    {
        var engine = BuildEngine(new InMemoryDelegationRegistry());
        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());

        engine.Decide(instance!, PrincipalWithRole("u.manager", "Manager"), WorkflowDecision.Approve);

        Assert.Equal(WorkflowInstanceStatus.Approved, instance!.Status);
    }
}
