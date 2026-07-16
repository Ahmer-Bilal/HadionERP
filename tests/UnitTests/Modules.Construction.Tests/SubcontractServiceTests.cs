using Modules.Construction.Application;
using Modules.Construction.Domain;
using Modules.MasterData.Contracts;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Construction.Tests;

public class SubcontractServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ApprovedProjectId = Guid.NewGuid();
    private static readonly Guid DraftProjectId = Guid.NewGuid();
    private static readonly Guid WbsRootId = Guid.NewGuid();
    private static readonly Guid WbsFoundationId = Guid.NewGuid();
    private static readonly Guid OtherProjectWbsId = Guid.NewGuid();
    private static readonly Guid ApprovedSubcontractorId = Guid.NewGuid();
    private static readonly Guid DraftSubcontractorId = Guid.NewGuid();
    private static readonly Guid NonSubcontractorVendorId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { SubcontractSecurity.MaintainerRoleKey },
            ["con.manager"] = new[] { SubcontractWorkflow.ApproverRoleKey },
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

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(ApprovedSubcontractorId, "Al Riyadh Steel Works", null, new[] { "Subcontractor" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(DraftSubcontractorId, "Not Yet Approved Sub", null, new[] { "Subcontractor" }, "Draft"));
        lookup.Add(new BusinessPartnerSummary(NonSubcontractorVendorId, "Steel Supplier Only", null, new[] { "Supplier" }, "Approved"));
        return lookup;
    }

    private static IReadOnlyList<CreateSubcontractLineRequest> BuildValidLines() => new[]
    {
        new CreateSubcontractLineRequest("SUB-001", "Formwork", null, "M2", 100m, 50m, WbsFoundationId),
    };

    private static CreateSubcontractRequest BuildValidRequest(Guid? projectId = null, Guid? contractId = null, Guid? subcontractorId = null) => new(
        projectId ?? ApprovedProjectId, contractId, subcontractorId ?? ApprovedSubcontractorId, 10m, 15m, 12, BuildValidLines());

    private static SubcontractService BuildService(
        out FakeSubcontractRepository repository, out IAuditLog auditLog, out FakeContractRepository contractRepository)
    {
        repository = new FakeSubcontractRepository();
        contractRepository = new FakeContractRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(SubcontractService.NumberRangeKey, "CON", "SUBCON")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { SubcontractWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { SubcontractSecurity.MaintainerRole, SubcontractSecurity.ApproverRole },
            new[] { SubcontractSecurity.MaintainerDuty, SubcontractSecurity.ApproverDuty });

        return new SubcontractService(
            repository, contractRepository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), BuildProjectLookup(), BuildBusinessPartnerLookup(), new FakeLookupCatalog());
    }

    [Fact]
    public async Task Create_assigns_a_document_number_and_computes_subcontract_value()
    {
        var service = BuildService(out _, out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"CON-SUBCON-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Single(created.Lines);
        Assert.Equal(5000m, created.SubcontractValue);
        Assert.Equal(5000m, created.NetPayableValue);
    }

    [Fact]
    public async Task Create_rejects_a_request_with_no_lines()
    {
        var service = BuildService(out _, out _, out _);
        var request = new CreateSubcontractRequest(ApprovedProjectId, null, ApprovedSubcontractorId, null, null, null, Array.Empty<CreateSubcontractLineRequest>());
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
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(DraftProjectId), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_line_whose_wbs_element_does_not_belong_to_the_project()
    {
        var service = BuildService(out _, out _, out _);
        var request = new CreateSubcontractRequest(
            ApprovedProjectId, null, ApprovedSubcontractorId, null, null, null,
            new[] { new CreateSubcontractLineRequest("SUB-001", "Formwork", null, "M2", 100m, 50m, OtherProjectWbsId) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_unit_of_measure()
    {
        var service = BuildService(out _, out _, out _);
        var request = new CreateSubcontractRequest(
            ApprovedProjectId, null, ApprovedSubcontractorId, null, null, null,
            new[] { new CreateSubcontractLineRequest("SUB-001", "Formwork", null, "NOT_A_UOM", 100m, 50m, WbsFoundationId) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_subcontractor()
    {
        var service = BuildService(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(subcontractorId: Guid.NewGuid()), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_subcontractor_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(subcontractorId: DraftSubcontractorId), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_business_partner_that_does_not_hold_the_Subcontractor_role()
    {
        var service = BuildService(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(subcontractorId: NonSubcontractorVendorId), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_contract()
    {
        var service = BuildService(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(contractId: Guid.NewGuid()), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_contract_that_belongs_to_a_different_project()
    {
        var service = BuildService(out _, out _, out var contractRepository);
        var contract = new Contract("ahmer.bilal", DraftProjectId, "LumpSum", null, null, null);
        contract.AssignNumber("CON-CONTR-2026-000001");
        contractRepository.Add(contract);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(contractId: contract.Id), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_contract_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out _, out var contractRepository);
        var contract = new Contract("ahmer.bilal", ApprovedProjectId, "LumpSum", null, null, null);
        contract.AssignNumber("CON-CONTR-2026-000001");
        contractRepository.Add(contract);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildValidRequest(contractId: contract.Id), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_accepts_a_valid_Approved_contract_on_the_same_project()
    {
        var service = BuildService(out _, out _, out var contractRepository);
        var contract = new Contract("ahmer.bilal", ApprovedProjectId, "LumpSum", null, null, null);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsFoundationId);
        contract.AssignNumber("CON-CONTR-2026-000001");
        contract.Submit("ahmer.bilal");
        contract.Approve("con.manager");
        contractRepository.Add(contract);

        var created = await service.CreateAsync(BuildValidRequest(contractId: contract.Id), "ahmer.bilal", "C001");
        Assert.Equal(contract.Id, created.ContractId);
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
    public async Task AddBackChargeAsync_before_approved_is_rejected()
    {
        var service = BuildService(out _, out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddBackChargeAsync(created.Id, new AddBackChargeRequest("Rework", 500m, new DateOnly(2026, 7, 16)), "ahmer.bilal"));
    }

    [Fact]
    public async Task AddBackChargeAsync_after_approved_reduces_net_payable_value()
    {
        var service = BuildService(out _, out _, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "con.manager");

        var updated = await service.AddBackChargeAsync(
            created.Id, new AddBackChargeRequest("Rework of defective formwork", 500m, new DateOnly(2026, 7, 16)), "ahmer.bilal");

        Assert.Single(updated.BackCharges);
        Assert.Equal(500m, updated.TotalBackCharges);
        Assert.Equal(4500m, updated.NetPayableValue);
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog, out _);
        var created = await service.CreateAsync(BuildValidRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "Subcontract", "Self"));
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
