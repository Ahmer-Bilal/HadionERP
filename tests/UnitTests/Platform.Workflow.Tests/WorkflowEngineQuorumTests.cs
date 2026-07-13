using Platform.Security;
using Platform.Workflow.Delegation;

namespace Platform.Workflow.Tests;

public class WorkflowEngineQuorumTests
{
    private static SecurityPrincipal PrincipalWithRole(string userId, string roleKey) =>
        new(userId, new[] { roleKey }, new Dictionary<string, IReadOnlySet<string>>());

    private static WorkflowEngine BuildCommitteeEngine()
    {
        var definition = new WorkflowDefinition("Capex_Approval", "InvestmentRequest", "Submit", new[]
        {
            new WorkflowStepDefinition("CommitteeApproval", "CommitteeMember", ApprovalQuorum.All)
        });
        var catalog = new InMemoryWorkflowDefinitionCatalog(new[] { definition });
        return new WorkflowEngine(catalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));
    }

    [Fact]
    public void All_quorum_requires_every_named_approver_before_advancing()
    {
        var engine = BuildCommitteeEngine();
        var instance = engine.Start("InvestmentRequest", "Submit", Guid.NewGuid(),
            requiredApproversByStep: new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["CommitteeApproval"] = new[] { "u.alice", "u.bob", "u.carol" }
            });

        Assert.NotNull(instance);

        engine.Decide(instance!, PrincipalWithRole("u.alice", "CommitteeMember"), WorkflowDecision.Approve);
        Assert.Equal(WorkflowInstanceStatus.Running, instance!.Status);

        engine.Decide(instance, PrincipalWithRole("u.bob", "CommitteeMember"), WorkflowDecision.Approve);
        Assert.Equal(WorkflowInstanceStatus.Running, instance.Status);

        engine.Decide(instance, PrincipalWithRole("u.carol", "CommitteeMember"), WorkflowDecision.Approve);
        Assert.Equal(WorkflowInstanceStatus.Approved, instance.Status);
    }

    [Fact]
    public void All_quorum_rejects_a_decision_from_someone_not_on_the_named_approver_list()
    {
        var engine = BuildCommitteeEngine();
        var instance = engine.Start("InvestmentRequest", "Submit", Guid.NewGuid(),
            requiredApproversByStep: new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["CommitteeApproval"] = new[] { "u.alice", "u.bob" }
            });

        // u.dave holds the CommitteeMember role (passes role-based eligibility) but isn't one of the
        // three named approvers for this specific instance — the instance itself must still reject it.
        var ex = Assert.Throws<ArgumentException>(() =>
            engine.Decide(instance!, PrincipalWithRole("u.dave", "CommitteeMember"), WorkflowDecision.Approve));

        Assert.Contains("not one of the required approvers", ex.Message);
    }

    [Fact]
    public void Any_quorum_advances_on_the_first_eligible_approval()
    {
        var definition = new WorkflowDefinition("PO_Approval", "PurchaseOrder", "Submit", new[]
        {
            new WorkflowStepDefinition("ManagerApproval", "Manager", ApprovalQuorum.Any)
        });
        var catalog = new InMemoryWorkflowDefinitionCatalog(new[] { definition });
        var engine = new WorkflowEngine(catalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var instance = engine.Start("PurchaseOrder", "Submit", Guid.NewGuid());
        engine.Decide(instance!, PrincipalWithRole("u.manager1", "Manager"), WorkflowDecision.Approve);

        Assert.Equal(WorkflowInstanceStatus.Approved, instance!.Status);
    }
}
