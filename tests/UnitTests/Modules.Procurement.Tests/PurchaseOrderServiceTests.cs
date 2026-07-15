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

public class PurchaseOrderServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ApprovedSupplierVendorId = Guid.NewGuid();
    private static readonly Guid DraftVendorId = Guid.NewGuid();
    private static readonly Guid UninvitedVendorId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid CostCenterId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { PurchaseOrderSecurity.MaintainerRoleKey },
            ["procurement.manager"] = new[] { PurchaseOrderWorkflow.ApproverRoleKey },
        });

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(ApprovedSupplierVendorId, "Gulf Falcon Trading Co", null, new[] { "Supplier" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(DraftVendorId, "New Vendor Co", null, new[] { "Supplier" }, "Draft"));
        lookup.Add(new BusinessPartnerSummary(UninvitedVendorId, "Other Supplier Co", null, new[] { "Supplier" }, "Approved"));
        return lookup;
    }

    private static (FakeItemLookup Items, FakeCostCenterLookup CostCenters) BuildMasterDataLookups()
    {
        var items = new FakeItemLookup();
        items.Add(new ItemSummary(ItemId, "CEM-001", "Portland Cement", "Bag", true));
        var costCenters = new FakeCostCenterLookup();
        costCenters.Add(new CostCenterSummary(CostCenterId, "CC-100", "Tower A Site", true, true));
        return (items, costCenters);
    }

    private static PurchaseRequisition BuildApprovedRequisition()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Cement for Tower A");
        requisition.AddLine(ItemId, CostCenterId, 100, 25);
        requisition.AssignNumber($"PROC-PR-{CurrentYear}-000001");
        requisition.Submit("ahmer.bilal");
        requisition.Approve("procurement.manager");
        return requisition;
    }

    private static RequestForQuotation BuildApprovedRfqWithFullQuote(
        PurchaseRequisition pr, Guid vendorId, decimal quotedUnitPrice)
    {
        var prLine = pr.Lines.Single();
        var rfq = new RequestForQuotation("ahmer.bilal", pr.Id, "Cement quotes");
        var line = rfq.AddLine(prLine.Id, prLine.ItemId, prLine.Quantity);
        rfq.InviteVendor(vendorId);
        rfq.AssignNumber($"PROC-RFQ-{CurrentYear}-000001");
        rfq.Submit("ahmer.bilal");
        rfq.RecordVendorQuote(vendorId, line.Id, quotedUnitPrice);
        rfq.Approve("procurement.manager");
        return rfq;
    }

    private static PurchaseOrderService BuildService(
        out FakePurchaseOrderRepository repository,
        out FakeRequestForQuotationRepository rfqRepository,
        out FakePurchaseRequisitionRepository prRepository,
        out FakeBudgetCheckService budgetCheckService,
        out IAuditLog auditLog)
    {
        repository = new FakePurchaseOrderRepository();
        rfqRepository = new FakeRequestForQuotationRepository();
        prRepository = new FakePurchaseRequisitionRepository();
        budgetCheckService = new FakeBudgetCheckService();
        var (items, costCenters) = BuildMasterDataLookups();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(PurchaseOrderService.NumberRangeKey, "PROC", "PO")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { PurchaseOrderWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { PurchaseOrderSecurity.MaintainerRole, PurchaseOrderSecurity.ApproverRole },
            new[] { PurchaseOrderSecurity.MaintainerDuty, PurchaseOrderSecurity.ApproverDuty });

        return new PurchaseOrderService(
            repository, rfqRepository, prRepository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), BuildBusinessPartnerLookup(), items, costCenters, budgetCheckService);
    }

    [Fact]
    public async Task Create_direct_assigns_a_document_number_and_lines()
    {
        var service = BuildService(out _, out _, out _, out _, out _);

        var request = new CreatePurchaseOrderRequest(
            ApprovedSupplierVendorId, null, new[] { new CreatePurchaseOrderLineRequest(ItemId, CostCenterId, 10, 5) });
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal($"PROC-PO-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Single(created.Lines);
        Assert.Equal(50, created.Total);
    }

    [Fact]
    public async Task Create_direct_rejects_an_unapproved_vendor()
    {
        var service = BuildService(out _, out _, out _, out _, out _);

        var request = new CreatePurchaseOrderRequest(
            DraftVendorId, null, new[] { new CreatePurchaseOrderLineRequest(ItemId, CostCenterId, 10, 5) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_direct_rejects_a_request_with_no_lines()
    {
        var service = BuildService(out _, out _, out _, out _, out _);

        var request = new CreatePurchaseOrderRequest(ApprovedSupplierVendorId, null, null);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_from_rfq_copies_lines_price_and_cost_center_from_the_source_pr()
    {
        var service = BuildService(out _, out var rfqRepo, out var prRepo, out _, out _);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);
        var rfq = BuildApprovedRfqWithFullQuote(pr, ApprovedSupplierVendorId, 27.5m);
        rfqRepo.Add(rfq);

        var request = new CreatePurchaseOrderRequest(ApprovedSupplierVendorId, rfq.Id, null);
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Single(created.Lines);
        Assert.Equal(ItemId, created.Lines[0].ItemId);
        Assert.Equal(CostCenterId, created.Lines[0].CostCenterId);
        Assert.Equal(27.5m, created.Lines[0].UnitPrice);
        Assert.Equal(rfq.Lines.Single().Id, created.Lines[0].RfqLineId);
        Assert.Equal(rfq.Id, created.RequestForQuotationId);
    }

    [Fact]
    public async Task Create_from_rfq_rejects_an_rfq_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out var rfqRepo, out var prRepo, out _, out _);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);
        var prLine = pr.Lines.Single();
        var rfq = new RequestForQuotation("ahmer.bilal", pr.Id, "Test");
        rfq.AddLine(prLine.Id, prLine.ItemId, prLine.Quantity);
        rfq.InviteVendor(ApprovedSupplierVendorId);
        rfq.AssignNumber($"PROC-RFQ-{CurrentYear}-000002");
        rfqRepo.Add(rfq);

        var request = new CreatePurchaseOrderRequest(ApprovedSupplierVendorId, rfq.Id, null);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_from_rfq_rejects_a_vendor_that_was_not_invited()
    {
        var service = BuildService(out _, out var rfqRepo, out var prRepo, out _, out _);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);
        var rfq = BuildApprovedRfqWithFullQuote(pr, ApprovedSupplierVendorId, 27.5m);
        rfqRepo.Add(rfq);

        var request = new CreatePurchaseOrderRequest(UninvitedVendorId, rfq.Id, null);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_from_rfq_rejects_a_vendor_that_has_not_quoted_every_line()
    {
        var service = BuildService(out _, out var rfqRepo, out var prRepo, out _, out _);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);
        var prLine = pr.Lines.Single();
        var rfq = new RequestForQuotation("ahmer.bilal", pr.Id, "Test");
        rfq.AddLine(prLine.Id, prLine.ItemId, prLine.Quantity);
        rfq.InviteVendor(ApprovedSupplierVendorId);
        rfq.AssignNumber($"PROC-RFQ-{CurrentYear}-000003");
        rfq.Submit("ahmer.bilal");
        rfq.Approve("procurement.manager");
        rfqRepo.Add(rfq);

        var request = new CreatePurchaseOrderRequest(ApprovedSupplierVendorId, rfq.Id, null);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_supplying_both_an_rfq_and_direct_lines()
    {
        var service = BuildService(out _, out var rfqRepo, out var prRepo, out _, out _);
        var pr = BuildApprovedRequisition();
        prRepo.Add(pr);
        var rfq = BuildApprovedRfqWithFullQuote(pr, ApprovedSupplierVendorId, 27.5m);
        rfqRepo.Add(rfq);

        var request = new CreatePurchaseOrderRequest(
            ApprovedSupplierVendorId, rfq.Id, new[] { new CreatePurchaseOrderLineRequest(ItemId, CostCenterId, 1, 1) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Submit_passes_when_budget_check_allows()
    {
        var service = BuildService(out _, out _, out _, out _, out _);
        var created = await service.CreateAsync(
            new CreatePurchaseOrderRequest(ApprovedSupplierVendorId, null, new[] { new CreatePurchaseOrderLineRequest(ItemId, CostCenterId, 10, 5) }),
            "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);

        var approved = await service.ApproveAsync(created.Id, "procurement.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task Submit_is_blocked_when_the_budget_check_denies_the_lines_cost_center()
    {
        var service = BuildService(out _, out _, out _, out var budgetCheckService, out _);
        budgetCheckService.Deny(CostCenterId);

        var created = await service.CreateAsync(
            new CreatePurchaseOrderRequest(ApprovedSupplierVendorId, null, new[] { new CreatePurchaseOrderLineRequest(ItemId, CostCenterId, 10, 5) }),
            "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync(created.Id, "ahmer.bilal"));

        var reloaded = await service.GetAsync(created.Id);
        Assert.Equal("Draft", reloaded!.Status);
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out _, out _, out _, out var auditLog);
        var created = await service.CreateAsync(
            new CreatePurchaseOrderRequest(ApprovedSupplierVendorId, null, new[] { new CreatePurchaseOrderLineRequest(ItemId, CostCenterId, 10, 5) }),
            "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "PurchaseOrder", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out _, out _, out _, out _);
        var request = new CreatePurchaseOrderRequest(
            ApprovedSupplierVendorId, null, new[] { new CreatePurchaseOrderLineRequest(ItemId, CostCenterId, 10, 5) });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(request, "procurement.manager", "C001"));
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _, out _, out _, out _, out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
