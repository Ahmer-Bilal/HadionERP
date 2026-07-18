using Modules.Finance.Application;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Finance.Tests;

public class BudgetServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid CostCenterId = Guid.NewGuid();
    private static readonly Guid InactiveCostCenterId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { BudgetSecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { BudgetWorkflow.ApproverRoleKey },
        });

    private static FakeCostCenterLookup BuildCostCenterLookup()
    {
        var lookup = new FakeCostCenterLookup();
        lookup.Add(new CostCenterSummary(CostCenterId, "CC-1000", "Head Office", IsPostable: true, IsActive: true));
        lookup.Add(new CostCenterSummary(InactiveCostCenterId, "CC-9999", "Closed Down", IsPostable: true, IsActive: false));
        return lookup;
    }

    private static BudgetService BuildService(out FakeBudgetRepository repository)
    {
        repository = new FakeBudgetRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(BudgetService.NumberRangeKey, "FIN", "BUD")
        });
        var auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { BudgetWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { BudgetSecurity.MaintainerRole, BudgetSecurity.ApproverRole },
            new[] { BudgetSecurity.MaintainerDuty, BudgetSecurity.ApproverDuty });

        return new BudgetService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), BuildCostCenterLookup());
    }

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(new CreateBudgetRequest(CostCenterId, CurrentYear, 50000m), "ahmer.bilal", "C001");

        Assert.Equal($"FIN-BUD-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(50000m, created.Amount);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_cost_center()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateBudgetRequest(Guid.NewGuid(), CurrentYear, 50000m), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_inactive_cost_center()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateBudgetRequest(InactiveCostCenterId, CurrentYear, 50000m), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_second_budget_for_the_same_cost_center_and_year()
    {
        var service = BuildService(out _);
        await service.CreateAsync(new CreateBudgetRequest(CostCenterId, CurrentYear, 50000m), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateBudgetRequest(CostCenterId, CurrentYear, 10000m), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task A_different_fiscal_year_for_the_same_cost_center_is_allowed()
    {
        var service = BuildService(out _);
        await service.CreateAsync(new CreateBudgetRequest(CostCenterId, CurrentYear, 50000m), "ahmer.bilal", "C001");

        var second = await service.CreateAsync(new CreateBudgetRequest(CostCenterId, CurrentYear + 1, 60000m), "ahmer.bilal", "C001");
        Assert.Equal(CurrentYear + 1, second.FiscalYear);
    }

    [Fact]
    public async Task A_rejected_budgets_cost_center_and_year_can_be_reused()
    {
        var service = BuildService(out _);
        var first = await service.CreateAsync(new CreateBudgetRequest(CostCenterId, CurrentYear, 50000m), "ahmer.bilal", "C001");
        await service.SubmitAsync(first.Id, "ahmer.bilal");
        await service.RejectAsync(first.Id, "finance.manager");

        var second = await service.CreateAsync(new CreateBudgetRequest(CostCenterId, CurrentYear, 60000m), "ahmer.bilal", "C001");
        Assert.Equal(60000m, second.Amount);
    }

    [Fact]
    public async Task Full_lifecycle_draft_to_approved()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(new CreateBudgetRequest(CostCenterId, CurrentYear, 50000m), "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);

        var approved = await service.ApproveAsync(created.Id, "finance.manager");
        Assert.Equal("Approved", approved.Status);
    }
}
