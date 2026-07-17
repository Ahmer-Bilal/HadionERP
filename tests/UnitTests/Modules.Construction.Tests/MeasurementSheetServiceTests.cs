using Modules.Construction.Application;
using Modules.Construction.Domain;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Construction.Tests;

public class MeasurementSheetServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ApprovedProjectId = Guid.NewGuid();
    private static readonly Guid OtherProjectId = Guid.NewGuid();
    private static readonly Guid WbsBillingId = Guid.NewGuid();
    private static readonly Guid WbsNonBillingId = Guid.NewGuid();
    private static readonly DateOnly PeriodStart = new(2026, 7, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 7, 31);

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { MeasurementSheetSecurity.MaintainerRoleKey },
            ["engineer"] = new[] { MeasurementSheetWorkflow.ApproverRoleKey },
        });

    private static FakeProjectLookup BuildProjectLookup()
    {
        var lookup = new FakeProjectLookup();
        lookup.Add(new ProjectSummary(
            ApprovedProjectId, "PM-PRJ-2026-000001", "Tower A Construction", null, null, "Approved",
            new[]
            {
                new WbsElementSummary(WbsBillingId, "1.1", "Foundation", null, true, true, true),
                new WbsElementSummary(WbsNonBillingId, "1.2", "Site Overhead", null, true, false, false),
            }));
        lookup.Add(new ProjectSummary(OtherProjectId, "PM-PRJ-2026-000002", "Tower B Construction", null, null, "Approved", Array.Empty<WbsElementSummary>()));
        return lookup;
    }

    private static Contract NewApprovedContract(FakeContractRepository contractRepository, decimal quantity = 100m, Guid? wbsElementId = null)
    {
        var contract = new Contract("ahmer.bilal", ApprovedProjectId, "LumpSum", null, null, null);
        var line = contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", quantity, 50m, wbsElementId ?? WbsBillingId);
        contract.AssignNumber("CON-CONTR-2026-000001");
        contract.Submit("ahmer.bilal");
        contract.Approve("con.manager");
        contractRepository.Add(contract);
        return contract;
    }

    private static MeasurementSheetService BuildService(
        out FakeMeasurementSheetRepository repository, out IAuditLog auditLog,
        out FakeContractRepository contractRepository, out FakeSubcontractRepository subcontractRepository)
    {
        repository = new FakeMeasurementSheetRepository();
        contractRepository = new FakeContractRepository();
        subcontractRepository = new FakeSubcontractRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(MeasurementSheetService.NumberRangeKey, "CON", "MEAS")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { MeasurementSheetWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { MeasurementSheetSecurity.MaintainerRole, MeasurementSheetSecurity.ApproverRole },
            new[] { MeasurementSheetSecurity.MaintainerDuty, MeasurementSheetSecurity.ApproverDuty });

        return new MeasurementSheetService(
            repository, contractRepository, subcontractRepository, numberRanges, new AuditRecorder(auditLog),
            workflowEngine, workflowInstances, new AuthorizationService(securityCatalog), BuildActorRoles(), BuildProjectLookup());
    }

    private static CreateMeasurementSheetRequest BuildValidRequest(Guid contractId, Guid boqLineId, decimal quantitySubmitted = 40m) => new(
        ApprovedProjectId, "Contract", contractId, PeriodStart, PeriodEnd, Notes: null,
        new[] { new CreateMeasurementLineRequest(boqLineId, quantitySubmitted, Remarks: null) });

    [Fact]
    public async Task Create_assigns_a_document_number()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository);

        var created = await service.CreateAsync(BuildValidRequest(contract.Id, contract.BoqLines.Single().Id), "ahmer.bilal", "C001");

        Assert.Equal($"CON-MEAS-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Single(created.Lines);
        Assert.Equal("Contract", created.CommercialDocumentType);
    }

    [Fact]
    public async Task Create_rejects_a_request_with_no_lines()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository);

        var request = new CreateMeasurementSheetRequest(
            ApprovedProjectId, "Contract", contract.Id, PeriodStart, PeriodEnd, null, Array.Empty<CreateMeasurementLineRequest>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_commercial_document_type()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository);

        var request = new CreateMeasurementSheetRequest(
            ApprovedProjectId, "NotAType", contract.Id, PeriodStart, PeriodEnd, null,
            new[] { new CreateMeasurementLineRequest(contract.BoqLines.Single().Id, 10m, null) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_contract_belonging_to_a_different_project()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = new Contract("ahmer.bilal", OtherProjectId, "LumpSum", null, null, null);
        contract.AssignNumber("CON-CONTR-2026-000002");
        contract.Submit("ahmer.bilal");
        contract.Approve("con.manager");
        contractRepository.Add(contract);

        var request = new CreateMeasurementSheetRequest(
            ApprovedProjectId, "Contract", contract.Id, PeriodStart, PeriodEnd, null,
            new[] { new CreateMeasurementLineRequest(Guid.NewGuid(), 10m, null) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_line_not_belonging_to_the_referenced_contract()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository);

        var request = new CreateMeasurementSheetRequest(
            ApprovedProjectId, "Contract", contract.Id, PeriodStart, PeriodEnd, null,
            new[] { new CreateMeasurementLineRequest(Guid.NewGuid(), 10m, null) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_line_whose_wbs_element_is_not_a_billing_element()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository, wbsElementId: WbsNonBillingId);

        var request = BuildValidRequest(contract.Id, contract.BoqLines.Single().Id);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_works_against_a_subcontract_too()
    {
        var service = BuildService(out _, out _, out _, out var subcontractRepository);
        var subcontract = new Subcontract("ahmer.bilal", ApprovedProjectId, null, Guid.NewGuid(), null, null, null);
        var line = subcontract.AddLine("SUB-001", "Formwork", null, "M2", 60m, 40m, WbsBillingId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");
        subcontract.Approve("con.manager");
        subcontractRepository.Add(subcontract);

        var request = new CreateMeasurementSheetRequest(
            ApprovedProjectId, "Subcontract", subcontract.Id, PeriodStart, PeriodEnd, null,
            new[] { new CreateMeasurementLineRequest(line.Id, 20m, null) });

        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");
        Assert.Equal("Subcontract", created.CommercialDocumentType);
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateAsync(BuildValidRequest(contract.Id, contract.BoqLines.Single().Id), "engineer", "C001"));
    }

    [Fact]
    public async Task Submit_then_certify_with_a_lower_quantity_than_submitted_reaches_approved()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository);
        var lineId = contract.BoqLines.Single().Id;
        var created = await service.CreateAsync(BuildValidRequest(contract.Id, lineId, quantitySubmitted: 40m), "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);

        var certifyRequest = new CertifyMeasurementSheetRequest(
            new[] { new CertifyMeasurementLineRequest(created.Lines.Single().Id, 35m) });
        var certified = await service.CertifyAsync(created.Id, certifyRequest, "engineer");

        Assert.Equal("Approved", certified.Status);
        Assert.Equal(35m, certified.Lines.Single().QuantityCertified);
    }

    [Fact]
    public async Task Certify_rejects_cumulative_over_measurement_beyond_the_lines_own_quantity()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository, quantity: 100m);
        var lineId = contract.BoqLines.Single().Id;

        // First sheet certifies the full 100.
        var sheet1 = await service.CreateAsync(BuildValidRequest(contract.Id, lineId, quantitySubmitted: 100m), "ahmer.bilal", "C001");
        await service.SubmitAsync(sheet1.Id, "ahmer.bilal");
        await service.CertifyAsync(sheet1.Id,
            new CertifyMeasurementSheetRequest(new[] { new CertifyMeasurementLineRequest(sheet1.Lines.Single().Id, 100m) }), "engineer");

        // A second sheet attempting to certify even 1 more against the same line must be rejected.
        var sheet2 = await service.CreateAsync(BuildValidRequest(contract.Id, lineId, quantitySubmitted: 10m), "ahmer.bilal", "C001");
        await service.SubmitAsync(sheet2.Id, "ahmer.bilal");

        await Assert.ThrowsAsync<ArgumentException>(() => service.CertifyAsync(sheet2.Id,
            new CertifyMeasurementSheetRequest(new[] { new CertifyMeasurementLineRequest(sheet2.Lines.Single().Id, 1m) }), "engineer"));
    }

    [Fact]
    public async Task Reject_after_submit_reaches_rejected()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository);
        var created = await service.CreateAsync(BuildValidRequest(contract.Id, contract.BoqLines.Single().Id), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var rejected = await service.RejectAsync(created.Id, "engineer");
        Assert.Equal("Rejected", rejected.Status);
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog, out var contractRepository, out _);
        var contract = NewApprovedContract(contractRepository);
        var created = await service.CreateAsync(BuildValidRequest(contract.Id, contract.BoqLines.Single().Id), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "MeasurementSheet", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _, out _, out _, out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
