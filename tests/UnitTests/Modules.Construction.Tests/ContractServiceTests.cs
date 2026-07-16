using Modules.Construction.Application;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Construction.Tests;

public class ContractServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ApprovedProjectId = Guid.NewGuid();
    private static readonly Guid DraftProjectId = Guid.NewGuid();
    private static readonly Guid WbsRootId = Guid.NewGuid();
    private static readonly Guid WbsFoundationId = Guid.NewGuid();
    private static readonly Guid OtherProjectWbsId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { ContractSecurity.MaintainerRoleKey },
            ["con.manager"] = new[] { ContractWorkflow.ApproverRoleKey },
        });

    private static FakeProjectLookup BuildProjectLookup()
    {
        var lookup = new FakeProjectLookup();
        lookup.Add(new ProjectSummary(
            ApprovedProjectId, "PM-PRJ-2026-000001", "Tower A Construction", null, null, "Approved",
            new[]
            {
                new WbsElementSummary(WbsRootId, "1.0", "Civil Works", null, false, false, false),
                new WbsElementSummary(WbsFoundationId, "1.1", "Foundation", null, true, true, false),
            }));
        lookup.Add(new ProjectSummary(DraftProjectId, "PM-PRJ-2026-000002", "Tower B Construction", null, null, "Draft", Array.Empty<WbsElementSummary>()));
        return lookup;
    }

    private static IReadOnlyList<CreateBoqLineRequest> BuildValidBoqLines() => new[]
    {
        new CreateBoqLineRequest("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsFoundationId),
    };

    private static CreateContractRequest BuildValidRequest(Guid? projectId = null) => new(
        projectId ?? ApprovedProjectId, "LumpSum", "30 days net", 10m, 12, BuildValidBoqLines());

    private static ContractService BuildService(
        out FakeContractRepository repository, out IAuditLog auditLog, out FakeProjectLookup projectLookup)
    {
        repository = new FakeContractRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(ContractService.NumberRangeKey, "CON", "CONTR")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { ContractWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { ContractSecurity.MaintainerRole, ContractSecurity.ApproverRole },
            new[] { ContractSecurity.MaintainerDuty, ContractSecurity.ApproverDuty });

        projectLookup = BuildProjectLookup();

        return new ContractService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), projectLookup, new FakeLookupCatalog());
    }

    [Fact]
    public async Task Create_assigns_a_document_number_and_computes_contract_value()
    {
        var service = BuildService(out _, out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"CON-CONTR-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Single(created.BoqLines);
        Assert.Equal(5000m, created.ContractValue);
    }

    [Fact]
    public async Task Create_rejects_a_request_with_no_boq_lines()
    {
        var service = BuildService(out _, out _, out _);
        var request = new CreateContractRequest(ApprovedProjectId, "LumpSum", null, null, null, Array.Empty<CreateBoqLineRequest>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_project()
    {
        var service = BuildService(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(Guid.NewGuid()), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_project_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out _, out _);
        var request = new CreateContractRequest(DraftProjectId, "LumpSum", null, null, null, BuildValidBoqLines());
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_boq_line_whose_wbs_element_does_not_belong_to_the_project()
    {
        var service = BuildService(out _, out _, out _);
        var request = new CreateContractRequest(
            ApprovedProjectId, "LumpSum", null, null, null,
            new[] { new CreateBoqLineRequest("BOQ-001", "Excavation", null, "M3", 100m, 50m, OtherProjectWbsId) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_contract_type()
    {
        var service = BuildService(out _, out _, out _);
        var request = new CreateContractRequest(ApprovedProjectId, "NotARealType", null, null, null, BuildValidBoqLines());
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_unit_of_measure()
    {
        var service = BuildService(out _, out _, out _);
        var request = new CreateContractRequest(
            ApprovedProjectId, "LumpSum", null, null, null,
            new[] { new CreateBoqLineRequest("BOQ-001", "Excavation", null, "NOT_A_UOM", 100m, 50m, WbsFoundationId) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out _, out _);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(BuildValidRequest(), "con.manager", "C001"));
    }

    [Fact]
    public async Task Submit_then_approve_reaches_approved()
    {
        var service = BuildService(out _, out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);

        var approved = await service.ApproveAsync(created.Id, "con.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task Reject_after_submit_reaches_rejected()
    {
        var service = BuildService(out _, out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var rejected = await service.RejectAsync(created.Id, "con.manager");
        Assert.Equal("Rejected", rejected.Status);
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "Contract", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _, out _, out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
