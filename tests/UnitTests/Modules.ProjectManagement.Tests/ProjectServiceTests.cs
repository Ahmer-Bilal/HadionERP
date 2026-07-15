using Modules.MasterData.Contracts;
using Modules.ProjectManagement.Application;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.ProjectManagement.Tests;

public class ProjectServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ApprovedClientId = Guid.NewGuid();
    private static readonly Guid DraftClientId = Guid.NewGuid();
    private static readonly Guid SupplierOnlyId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { ProjectSecurity.MaintainerRoleKey },
            ["pm.manager"] = new[] { ProjectWorkflow.ApproverRoleKey },
        });

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(ApprovedClientId, "Aramco", null, new[] { "Client" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(DraftClientId, "New Client Co", null, new[] { "Client" }, "Draft"));
        lookup.Add(new BusinessPartnerSummary(SupplierOnlyId, "Gulf Falcon Trading Co", null, new[] { "Supplier" }, "Approved"));
        return lookup;
    }

    private static CreateProjectRequest BuildValidRequest(Guid? customerId = null) => new(
        "Tower A Construction", null, customerId, new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31),
        new[]
        {
            new CreateWbsElementRequest(0, null, "1.0", "Civil Works", null, false, false, false),
            new CreateWbsElementRequest(1, 0, "1.1", "Foundation", null, true, true, false),
        });

    private static ProjectService BuildService(
        out FakeProjectRepository repository, out IAuditLog auditLog)
    {
        repository = new FakeProjectRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(ProjectService.NumberRangeKey, "PM", "PRJ")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { ProjectWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { ProjectSecurity.MaintainerRole, ProjectSecurity.ApproverRole },
            new[] { ProjectSecurity.MaintainerDuty, ProjectSecurity.ApproverDuty });

        return new ProjectService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), BuildBusinessPartnerLookup());
    }

    [Fact]
    public async Task Create_assigns_a_document_number_and_builds_the_wbs_hierarchy()
    {
        var service = BuildService(out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"PM-PRJ-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(2, created.WbsElements.Count);
        var root = created.WbsElements.Single(w => w.Code == "1.0");
        var child = created.WbsElements.Single(w => w.Code == "1.1");
        Assert.Null(root.ParentWbsElementId);
        Assert.Equal(root.Id, child.ParentWbsElementId);
    }

    [Fact]
    public async Task Create_rejects_a_request_with_no_wbs_elements()
    {
        var service = BuildService(out _, out _);
        var request = new CreateProjectRequest("Tower A", null, null, null, null, Array.Empty<CreateWbsElementRequest>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_child_element_whose_parent_tempid_has_not_appeared_yet()
    {
        var service = BuildService(out _, out _);
        var request = new CreateProjectRequest("Tower A", null, null, null, null, new[]
        {
            new CreateWbsElementRequest(0, 5, "1.1", "Foundation", null, true, true, false),
        });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_accepts_an_approved_client_as_customer()
    {
        var service = BuildService(out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(ApprovedClientId), "ahmer.bilal", "C001");
        Assert.Equal(ApprovedClientId, created.CustomerId);
    }

    [Fact]
    public async Task Create_rejects_a_customer_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(DraftClientId), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_business_partner_that_does_not_hold_the_Client_role()
    {
        var service = BuildService(out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(SupplierOnlyId), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out _);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(BuildValidRequest(), "pm.manager", "C001"));
    }

    [Fact]
    public async Task Submit_then_approve_reaches_approved()
    {
        var service = BuildService(out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);

        var approved = await service.ApproveAsync(created.Id, "pm.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task Reject_after_submit_reaches_rejected()
    {
        var service = BuildService(out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var rejected = await service.RejectAsync(created.Id, "pm.manager");
        Assert.Equal("Rejected", rejected.Status);
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "Project", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _, out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
