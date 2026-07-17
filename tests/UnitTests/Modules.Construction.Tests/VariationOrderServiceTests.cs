using Modules.Construction.Application;
using Modules.Construction.Domain;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Construction.Tests;

public class VariationOrderServiceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid WbsElementId = Guid.NewGuid();
    private static readonly Guid OtherWbsElementId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { VariationOrderSecurity.MaintainerRoleKey },
            ["con.manager"] = new[] { VariationOrderWorkflow.ApproverRoleKey },
        });

    private static FakeProjectLookup BuildProjectLookup()
    {
        var lookup = new FakeProjectLookup();
        lookup.Add(new ProjectSummary(ProjectId, "PM-PRJ-2026-000001", "Tower A Construction", null, Guid.NewGuid(), "Approved",
            new[]
            {
                new WbsElementSummary(WbsElementId, "WBS-001", "Structure", null, true, true, true),
                new WbsElementSummary(OtherWbsElementId, "WBS-002", "MEP", null, true, true, true),
            }));
        return lookup;
    }

    private static Contract NewApprovedContract(FakeContractRepository contractRepository, decimal quantity = 100m, decimal rate = 50m)
    {
        var contract = new Contract("ahmer.bilal", ProjectId, "LumpSum", null, 15m, null);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", quantity, rate, WbsElementId);
        contract.AssignNumber("CON-CONTR-2026-000001");
        contract.Submit("ahmer.bilal");
        contract.Approve("con.manager");
        contractRepository.Add(contract);
        return contract;
    }

    private static VariationOrderService BuildService(
        out FakeVariationOrderRepository repository, out FakeContractRepository contractRepository,
        out FakeSubcontractRepository subcontractRepository, out IAuditLog auditLog)
    {
        repository = new FakeVariationOrderRepository();
        contractRepository = new FakeContractRepository();
        subcontractRepository = new FakeSubcontractRepository();
        auditLog = new InMemoryAuditLog();

        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(VariationOrderService.NumberRangeKey, "CON", "VO")
        });
        var workflowInstances = new FakeWorkflowInstanceRepository();
        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { VariationOrderWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));
        var securityCatalog = new InMemorySecurityCatalog(
            new[] { VariationOrderSecurity.MaintainerRole, VariationOrderSecurity.ApproverRole },
            new[] { VariationOrderSecurity.MaintainerDuty, VariationOrderSecurity.ApproverDuty });

        return new VariationOrderService(
            repository, contractRepository, subcontractRepository, numberRanges,
            new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), BuildProjectLookup());
    }

    [Fact]
    public async Task Create_with_an_adjustment_line_snapshots_the_rate_from_the_document_line()
    {
        var service = BuildService(out _, out var contractRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository, quantity: 100m, rate: 50m);
        var lineId = contract.BoqLines.Single().Id;

        var request = new CreateVariationOrderRequest(
            ProjectId, "Contract", contract.Id, "Additional excavation scope",
            new[] { new CreateVariationOrderLineRequest(lineId, 20m) });
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal("Draft", created.Status);
        Assert.Equal($"CON-VO-{DateTimeOffset.UtcNow.Year}-000001", created.DocumentNumber);
        var line = Assert.Single(created.Lines);
        Assert.Equal(50m, line.Rate);
        Assert.Equal(1000m, created.TotalValue); // 20 * 50
    }

    [Fact]
    public async Task Create_rejects_a_line_referencing_an_unknown_document_line()
    {
        var service = BuildService(out _, out var contractRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);

        var request = new CreateVariationOrderRequest(
            ProjectId, "Contract", contract.Id, "Bogus line",
            new[] { new CreateVariationOrderLineRequest(Guid.NewGuid(), 10m) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_new_line_with_a_WBS_element_not_in_the_project()
    {
        var service = BuildService(out _, out var contractRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);

        var request = new CreateVariationOrderRequest(
            ProjectId, "Contract", contract.Id, "New scope",
            new[] { new CreateVariationOrderLineRequest(null, 10m, "BOQ-002", "New Item", null, "M2", Guid.NewGuid(), 30m) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Submit_then_approve_increases_the_contracts_BOQ_line_quantity()
    {
        var service = BuildService(out _, out var contractRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository, quantity: 100m, rate: 50m);
        var lineId = contract.BoqLines.Single().Id;

        var request = new CreateVariationOrderRequest(
            ProjectId, "Contract", contract.Id, "Additional excavation scope",
            new[] { new CreateVariationOrderLineRequest(lineId, 20m) });
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        await service.SubmitAsync(created.Id, "ahmer.bilal");
        var approved = await service.ApproveAsync(created.Id, "con.manager");

        Assert.Equal("Approved", approved.Status);
        Assert.Equal(120m, contract.BoqLines.Single().Quantity); // 100 + 20
    }

    [Fact]
    public async Task Approve_adds_a_wholly_new_BOQ_line_to_the_contract()
    {
        var service = BuildService(out _, out var contractRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);

        var request = new CreateVariationOrderRequest(
            ProjectId, "Contract", contract.Id, "New scope item",
            new[] { new CreateVariationOrderLineRequest(null, 10m, "BOQ-002", "New Item", null, "M2", OtherWbsElementId, 30m) });
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "con.manager");

        Assert.Equal(2, contract.BoqLines.Count);
        var newLine = contract.BoqLines.Single(l => l.Code == "BOQ-002");
        Assert.Equal(10m, newLine.Quantity);
        Assert.Equal(30m, newLine.Rate);
        Assert.Equal(OtherWbsElementId, newLine.WbsElementId);
    }

    [Fact]
    public async Task Reject_leaves_the_contracts_BOQ_line_quantity_unchanged()
    {
        var service = BuildService(out _, out var contractRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository, quantity: 100m, rate: 50m);
        var lineId = contract.BoqLines.Single().Id;

        var request = new CreateVariationOrderRequest(
            ProjectId, "Contract", contract.Id, "Additional excavation scope",
            new[] { new CreateVariationOrderLineRequest(lineId, 20m) });
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        await service.SubmitAsync(created.Id, "ahmer.bilal");
        var rejected = await service.RejectAsync(created.Id, "con.manager");

        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal(100m, contract.BoqLines.Single().Quantity);
    }

    [Fact]
    public async Task Approve_adjusts_a_subcontracts_line_quantity()
    {
        var service = BuildService(out _, out _, out var subcontractRepository, out _);
        var subcontract = new Subcontract("ahmer.bilal", ProjectId, null, Guid.NewGuid(), 10m, null, null);
        var line = subcontract.AddLine("SUB-001", "Formwork", null, "M2", 60m, 40m, WbsElementId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");
        subcontract.Approve("con.manager");
        subcontractRepository.Add(subcontract);

        var request = new CreateVariationOrderRequest(
            ProjectId, "Subcontract", subcontract.Id, "Additional formwork",
            new[] { new CreateVariationOrderLineRequest(line.Id, -10m) }); // an omission
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        await service.SubmitAsync(created.Id, "ahmer.bilal");
        var approved = await service.ApproveAsync(created.Id, "con.manager");

        Assert.Equal("Approved", approved.Status);
        Assert.Equal(50m, subcontract.Lines.Single().Quantity); // 60 - 10
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out var contractRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        var lineId = contract.BoqLines.Single().Id;

        var request = new CreateVariationOrderRequest(
            ProjectId, "Contract", contract.Id, "Scope change",
            new[] { new CreateVariationOrderLineRequest(lineId, 5m) });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(request, "con.manager", "C001"));
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _, out _, out _, out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
