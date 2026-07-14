using Modules.MasterData.Contracts;
using Modules.Procurement.Application;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Procurement.Tests;

public class PurchaseRequisitionServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid RebarItemId = Guid.NewGuid();
    private static readonly Guid InactiveItemId = Guid.NewGuid();
    private static readonly Guid CostCenterId = Guid.NewGuid();
    private static readonly Guid InactiveCostCenterId = Guid.NewGuid();
    private static readonly Guid HeaderCostCenterId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { PurchaseRequisitionSecurity.MaintainerRoleKey },
            ["procurement.manager"] = new[] { PurchaseRequisitionWorkflow.ApproverRoleKey },
        });

    private static FakeItemLookup BuildItemLookup()
    {
        var lookup = new FakeItemLookup();
        lookup.Add(new ItemSummary(RebarItemId, "MAT-1010", "Rebar 12mm", "TON", IsActive: true));
        lookup.Add(new ItemSummary(InactiveItemId, "MAT-9999", "Discontinued Item", "EA", IsActive: false));
        return lookup;
    }

    private static FakeCostCenterLookup BuildCostCenterLookup()
    {
        var lookup = new FakeCostCenterLookup();
        lookup.Add(new CostCenterSummary(CostCenterId, "CC-1000", "Tower A Site", IsPostable: true, IsActive: true));
        lookup.Add(new CostCenterSummary(InactiveCostCenterId, "CC-9999", "Closed Site", IsPostable: true, IsActive: false));
        lookup.Add(new CostCenterSummary(HeaderCostCenterId, "CC-0000", "All Sites", IsPostable: false, IsActive: true));
        return lookup;
    }

    private static PurchaseRequisitionService BuildService(out FakePurchaseRequisitionRepository repository) =>
        BuildService(out repository, out _);

    private static PurchaseRequisitionService BuildService(
        out FakePurchaseRequisitionRepository repository, out IAuditLog auditLog)
    {
        repository = new FakePurchaseRequisitionRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(PurchaseRequisitionService.NumberRangeKey, "PROC", "PR")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { PurchaseRequisitionWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { PurchaseRequisitionSecurity.MaintainerRole, PurchaseRequisitionSecurity.ApproverRole },
            new[] { PurchaseRequisitionSecurity.MaintainerDuty, PurchaseRequisitionSecurity.ApproverDuty });

        return new PurchaseRequisitionService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(),
            BuildItemLookup(), BuildCostCenterLookup());
    }

    private static CreatePurchaseRequisitionRequest ValidRequest() => new(
        "Rebar for Tower A", new[] { new CreatePurchaseRequisitionLineRequest(RebarItemId, CostCenterId, 10, 500) });

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"PROC-PR-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Single(created.Lines);
        Assert.Equal(5000, created.EstimatedTotal);
    }

    [Fact]
    public async Task Create_rejects_a_line_referencing_an_unknown_item()
    {
        var service = BuildService(out _);
        var request = new CreatePurchaseRequisitionRequest("Test",
            new[] { new CreatePurchaseRequisitionLineRequest(Guid.NewGuid(), CostCenterId, 1, 10) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_line_referencing_an_inactive_item()
    {
        var service = BuildService(out _);
        var request = new CreatePurchaseRequisitionRequest("Test",
            new[] { new CreatePurchaseRequisitionLineRequest(InactiveItemId, CostCenterId, 1, 10) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_cost_center()
    {
        var service = BuildService(out _);
        var request = new CreatePurchaseRequisitionRequest("Test",
            new[] { new CreatePurchaseRequisitionLineRequest(RebarItemId, Guid.NewGuid(), 1, 10) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_inactive_cost_center()
    {
        var service = BuildService(out _);
        var request = new CreatePurchaseRequisitionRequest("Test",
            new[] { new CreatePurchaseRequisitionLineRequest(RebarItemId, InactiveCostCenterId, 1, 10) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_non_postable_header_cost_center()
    {
        var service = BuildService(out _);
        var request = new CreatePurchaseRequisitionRequest("Test",
            new[] { new CreatePurchaseRequisitionLineRequest(RebarItemId, HeaderCostCenterId, 1, 10) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_request_with_no_lines()
    {
        var service = BuildService(out _);
        var request = new CreatePurchaseRequisitionRequest("Test", Array.Empty<CreatePurchaseRequisitionLineRequest>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(ValidRequest(), "procurement.manager", "C001"));
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "PurchaseRequisition", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task Submit_approve_reaches_approved_with_workflow()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var approved = await service.ApproveAsync(created.Id, "procurement.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task Reject_after_submit_reaches_rejected()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var rejected = await service.RejectAsync(created.Id, "procurement.manager");
        Assert.Equal("Rejected", rejected.Status);
    }

    [Fact]
    public async Task ApproveAsync_throws_for_an_actor_with_no_Approver_role()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveAsync(created.Id, "ahmer.bilal"));
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
