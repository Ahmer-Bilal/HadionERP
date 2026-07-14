using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Infrastructure;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.MasterData.IntegrationTests;

/// <summary>
/// Proves <see cref="EfWorkflowInstanceRepository"/> actually persists a <see cref="WorkflowInstance"/> —
/// and, critically, that a decision made against an instance loaded through one <see cref="MasterDataDbContext"/>
/// is still there after reloading through a completely fresh one. This is the specific risk the jsonb
/// conversions in <see cref="MasterDataDbContext"/> carry: <c>WorkflowInstance.Decide</c> mutates its
/// history/approved-by-step collections IN PLACE rather than replacing them, so without an explicit
/// <c>ValueComparer</c> configured on those properties, EF Core's default reference-equality change
/// detection could see "the same object reference as before" and silently skip writing the decision —
/// exactly the kind of bug that only a real database round-trip test catches, not a unit test with a fake
/// repository.
/// </summary>
public class WorkflowInstancePersistenceTests : IAsyncLifetime
{
    private static readonly WorkflowStepDefinition Step = new("Approve", "MasterData.ApproveBusinessPartner");
    private static readonly WorkflowDefinition Definition = new("BusinessPartner.Onboarding.v1", "BusinessPartner", "Submit", new[] { Step });

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static WorkflowEngine NewEngine() =>
        new(new InMemoryWorkflowDefinitionCatalog(new[] { Definition }), new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

    [Fact]
    public async Task A_started_instance_reads_back_identically_through_a_fresh_DbContext()
    {
        var engine = NewEngine();
        var businessObjectId = Guid.NewGuid();
        var instance = engine.Start("BusinessPartner", "Submit", businessObjectId)!;

        await using (var writeContext = TestDatabase.CreateContext())
        {
            writeContext.WorkflowInstances.Add(instance);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.WorkflowInstances.FirstOrDefaultAsync(i => i.Id == instance.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(WorkflowInstanceStatus.Running, reloaded!.Status);
        Assert.Equal("BusinessPartner.Onboarding.v1", reloaded.DefinitionKey);
        Assert.Equal(businessObjectId, reloaded.BusinessObjectId);
        Assert.Single(reloaded.ApplicableSteps);
        Assert.Equal("Approve", reloaded.ApplicableSteps[0].StepId);
    }

    [Fact]
    public async Task A_decision_made_after_reloading_is_still_there_after_reloading_again()
    {
        var engine = NewEngine();
        var businessObjectId = Guid.NewGuid();
        var instance = engine.Start("BusinessPartner", "Submit", businessObjectId)!;

        await using (var writeContext = TestDatabase.CreateContext())
        {
            writeContext.WorkflowInstances.Add(instance);
            await writeContext.SaveChangesAsync();
        }

        // A completely separate unit of work decides it — the real-world shape (Submit today, Approve
        // days later, in a different HTTP request).
        await using (var decideContext = TestDatabase.CreateContext())
        {
            var loaded = await decideContext.WorkflowInstances.FirstAsync(i => i.Id == instance.Id);
            var principal = new SecurityPrincipal("finance.manager", new[] { "MasterData.ApproveBusinessPartner" }, new Dictionary<string, IReadOnlySet<string>>());
            engine.Decide(loaded, principal, WorkflowDecision.Approve, comment: "Looks correct");
            await decideContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.WorkflowInstances.FirstAsync(i => i.Id == instance.Id);

        Assert.Equal(WorkflowInstanceStatus.Approved, reloaded.Status);
        var decision = Assert.Single(reloaded.History);
        Assert.Equal("finance.manager", decision.ActorUserId);
        Assert.Equal(WorkflowDecision.Approve, decision.Decision);
        Assert.Equal("Looks correct", decision.Comment);
    }

    [Fact]
    public async Task EfWorkflowInstanceRepository_GetActiveAsync_finds_the_running_instance_for_a_business_object()
    {
        var engine = NewEngine();
        var businessObjectId = Guid.NewGuid();
        var instance = engine.Start("BusinessPartner", "Submit", businessObjectId)!;

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var repository = new EfWorkflowInstanceRepository(writeContext);
            repository.Add(instance);
            await repository.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var repository2 = new EfWorkflowInstanceRepository(readContext);
        var active = await repository2.GetActiveAsync("BusinessPartner", businessObjectId);

        Assert.NotNull(active);
        Assert.Equal(instance.Id, active!.Id);
    }
}
