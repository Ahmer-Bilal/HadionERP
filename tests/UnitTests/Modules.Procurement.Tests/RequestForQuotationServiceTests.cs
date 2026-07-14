using Modules.MasterData.Contracts;
using Modules.Procurement.Application;
using Modules.Procurement.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Procurement.Tests;

public class RequestForQuotationServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ApprovedSupplierVendorId = Guid.NewGuid();
    private static readonly Guid DraftVendorId = Guid.NewGuid();
    private static readonly Guid ClientOnlyVendorId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { RequestForQuotationSecurity.MaintainerRoleKey },
            ["procurement.manager"] = new[] { RequestForQuotationWorkflow.ApproverRoleKey },
        });

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(ApprovedSupplierVendorId, "Gulf Falcon Trading Co", null, new[] { "Supplier" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(DraftVendorId, "New Vendor Co", null, new[] { "Supplier" }, "Draft"));
        lookup.Add(new BusinessPartnerSummary(ClientOnlyVendorId, "Client Only Co", null, new[] { "Client" }, "Approved"));
        return lookup;
    }

    private static PurchaseRequisition BuildApprovedRequisition()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Cement for Tower A");
        requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 100, 25);
        requisition.AssignNumber($"PROC-PR-{CurrentYear}-000001");
        requisition.Submit("ahmer.bilal");
        requisition.Approve("procurement.manager");
        return requisition;
    }

    private static PurchaseRequisition BuildDraftRequisition()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Test");
        requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, 1);
        requisition.AssignNumber($"PROC-PR-{CurrentYear}-000002");
        return requisition;
    }

    private static RequestForQuotationService BuildService(
        out FakeRequestForQuotationRepository repository, out FakePurchaseRequisitionRepository purchaseRequisitionRepository) =>
        BuildService(out repository, out purchaseRequisitionRepository, out _);

    private static RequestForQuotationService BuildService(
        out FakeRequestForQuotationRepository repository,
        out FakePurchaseRequisitionRepository purchaseRequisitionRepository,
        out IAuditLog auditLog)
    {
        repository = new FakeRequestForQuotationRepository();
        purchaseRequisitionRepository = new FakePurchaseRequisitionRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(RequestForQuotationService.NumberRangeKey, "PROC", "RFQ")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { RequestForQuotationWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { RequestForQuotationSecurity.MaintainerRole, RequestForQuotationSecurity.ApproverRole },
            new[] { RequestForQuotationSecurity.MaintainerDuty, RequestForQuotationSecurity.ApproverDuty });

        return new RequestForQuotationService(
            repository, purchaseRequisitionRepository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), BuildBusinessPartnerLookup());
    }

    [Fact]
    public async Task Create_assigns_a_document_number_and_copies_lines_from_the_requisition()
    {
        var service = BuildService(out _, out var prRepo);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);

        var request = new CreateRequestForQuotationRequest(pr.Id, "Cement quotes", new[] { ApprovedSupplierVendorId });
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal($"PROC-RFQ-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Single(created.Lines);
        Assert.Equal(pr.Lines.Single().ItemId, created.Lines[0].ItemId);
        Assert.Single(created.InvitedVendors);
    }

    [Fact]
    public async Task Create_rejects_a_requisition_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out var prRepo);
        var pr = BuildDraftRequisition();
        prRepo.Add(pr);

        var request = new CreateRequestForQuotationRequest(pr.Id, "Test", new[] { ApprovedSupplierVendorId });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_requisition()
    {
        var service = BuildService(out _, out _);
        var request = new CreateRequestForQuotationRequest(Guid.NewGuid(), "Test", new[] { ApprovedSupplierVendorId });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_request_with_no_invited_vendors()
    {
        var service = BuildService(out _, out var prRepo);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);

        var request = new CreateRequestForQuotationRequest(pr.Id, "Test", Array.Empty<Guid>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_vendor_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out var prRepo);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);

        var request = new CreateRequestForQuotationRequest(pr.Id, "Test", new[] { DraftVendorId });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_vendor_with_no_quote_eligible_role()
    {
        var service = BuildService(out _, out var prRepo);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);

        var request = new CreateRequestForQuotationRequest(pr.Id, "Test", new[] { ClientOnlyVendorId });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out var prRepo);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);

        var request = new CreateRequestForQuotationRequest(pr.Id, "Test", new[] { ApprovedSupplierVendorId });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(request, "procurement.manager", "C001"));
    }

    [Fact]
    public async Task Submit_record_quote_approve_reaches_approved_with_a_recorded_quote()
    {
        var service = BuildService(out _, out var prRepo);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);

        var created = await service.CreateAsync(
            new CreateRequestForQuotationRequest(pr.Id, "Test", new[] { ApprovedSupplierVendorId }), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var quoted = await service.RecordVendorQuoteAsync(
            created.Id, new RecordVendorQuoteRequest(ApprovedSupplierVendorId, created.Lines[0].Id, 27.5m), "ahmer.bilal");
        Assert.Single(quoted.VendorQuoteLines);
        Assert.Equal(27.5m, quoted.VendorQuoteLines[0].QuotedUnitPrice);

        var approved = await service.ApproveAsync(created.Id, "procurement.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task RecordVendorQuoteAsync_rejects_a_vendor_that_was_not_invited()
    {
        var service = BuildService(out _, out var prRepo);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);

        var created = await service.CreateAsync(
            new CreateRequestForQuotationRequest(pr.Id, "Test", new[] { ApprovedSupplierVendorId }), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        await Assert.ThrowsAsync<ArgumentException>(() => service.RecordVendorQuoteAsync(
            created.Id, new RecordVendorQuoteRequest(Guid.NewGuid(), created.Lines[0].Id, 10), "ahmer.bilal"));
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var prRepo, out var auditLog);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);

        var created = await service.CreateAsync(
            new CreateRequestForQuotationRequest(pr.Id, "Test", new[] { ApprovedSupplierVendorId }), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "RequestForQuotation", "Self"));
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
