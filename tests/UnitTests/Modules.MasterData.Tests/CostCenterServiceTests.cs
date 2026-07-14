using Modules.MasterData.Application;
using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.MasterData.Tests;

public class CostCenterServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { CostCenterSecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { CostCenterWorkflow.ApproverRoleKey },
        });

    private static CostCenterService BuildService(out FakeCostCenterRepository repository) =>
        BuildService(out repository, out _);

    private static CostCenterService BuildService(out FakeCostCenterRepository repository, out IAuditLog auditLog)
    {
        repository = new FakeCostCenterRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(CostCenterService.NumberRangeKey, "MD", "CC")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { CostCenterWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { CostCenterSecurity.MaintainerRole, CostCenterSecurity.ApproverRole },
            new[] { CostCenterSecurity.MaintainerDuty, CostCenterSecurity.ApproverDuty });

        return new CostCenterService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles());
    }

    private static CreateCostCenterRequest ValidRequest(string code = "CC-1000") =>
        new(code, "Head Office");

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"MD-CC-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal("CC-1000", created.CostCenterCode);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_cost_center_code()
    {
        var service = BuildService(out _);
        await service.CreateAsync(ValidRequest("CC-1000"), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(ValidRequest("CC-1000"), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "CostCenter", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task Submit_then_approve_reaches_approved_with_workflow()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var approved = await service.ApproveAsync(created.Id, "finance.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task ApproveAsync_throws_for_an_actor_with_no_Approver_role()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveAsync(created.Id, "ahmer.bilal"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(ValidRequest(), "finance.manager", "C001"));
    }

    [Fact]
    public async Task Update_changes_name_and_persists()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var updated = await service.UpdateAsync(created.Id,
            new UpdateCostCenterRequest("Corporate Head Office", null, null, true, true), "ahmer.bilal");

        Assert.Equal("Corporate Head Office", updated.CostCenterName);
    }

    [Fact]
    public async Task CreateAsync_persists_the_Arabic_name_when_provided()
    {
        var service = BuildService(out _);
        var request = new CreateCostCenterRequest("CC-1000", "Head Office", "المكتب الرئيسي");

        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal("المكتب الرئيسي", created.CostCenterNameArabic);
    }

    [Fact]
    public async Task Create_with_a_parent_persists_the_hierarchy_link()
    {
        var service = BuildService(out _);
        var parent = await service.CreateAsync(ValidRequest("CC-1000"), "ahmer.bilal", "C001");
        var child = await service.CreateAsync(
            new CreateCostCenterRequest("CC-1010", "Finance Department", ParentCostCenterId: parent.Id),
            "ahmer.bilal", "C001");

        Assert.Equal(parent.Id, child.ParentCostCenterId);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
