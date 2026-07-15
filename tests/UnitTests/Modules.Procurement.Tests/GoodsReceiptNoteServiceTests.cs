using Modules.Procurement.Application;
using Modules.Procurement.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Procurement.Tests;

public class GoodsReceiptNoteServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid VendorId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid CostCenterId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { GoodsReceiptNoteSecurity.MaintainerRoleKey },
            ["procurement.manager"] = new[] { GoodsReceiptNoteWorkflow.ApproverRoleKey },
        });

    private static PurchaseOrder BuildApprovedPurchaseOrder(decimal quantity = 100, decimal unitPrice = 10)
    {
        var po = new PurchaseOrder("ahmer.bilal", VendorId);
        po.AddLine(ItemId, CostCenterId, quantity, unitPrice);
        po.AssignNumber($"PROC-PO-{CurrentYear}-000001");
        po.Submit("ahmer.bilal");
        po.Approve("procurement.manager");
        return po;
    }

    private static GoodsReceiptNoteService BuildService(
        out FakeGoodsReceiptNoteRepository repository,
        out FakePurchaseOrderRepository poRepository,
        out IAuditLog auditLog)
    {
        repository = new FakeGoodsReceiptNoteRepository();
        poRepository = new FakePurchaseOrderRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(GoodsReceiptNoteService.NumberRangeKey, "PROC", "GRN")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { GoodsReceiptNoteWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { GoodsReceiptNoteSecurity.MaintainerRole, GoodsReceiptNoteSecurity.ApproverRole },
            new[] { GoodsReceiptNoteSecurity.MaintainerDuty, GoodsReceiptNoteSecurity.ApproverDuty });

        return new GoodsReceiptNoteService(
            repository, poRepository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles());
    }

    [Fact]
    public async Task Create_assigns_a_document_number_and_lines()
    {
        var service = BuildService(out _, out var poRepo, out _);
        var po = BuildApprovedPurchaseOrder();
        poRepo.Add(po);

        var request = new CreateGoodsReceiptNoteRequest(
            po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(po.Lines.Single().Id, 40) });
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal($"PROC-GRN-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Single(created.Lines);
        Assert.Equal(400, created.ReceivedValue);
        Assert.Equal(10, created.Lines[0].UnitPrice);
    }

    [Fact]
    public async Task Create_rejects_a_purchase_order_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out var poRepo, out _);
        var po = new PurchaseOrder("ahmer.bilal", VendorId);
        po.AddLine(ItemId, CostCenterId, 10, 10);
        po.AssignNumber($"PROC-PO-{CurrentYear}-000002");
        poRepo.Add(po);

        var request = new CreateGoodsReceiptNoteRequest(
            po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(po.Lines.Single().Id, 5) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_line_that_does_not_belong_to_the_purchase_order()
    {
        var service = BuildService(out _, out var poRepo, out _);
        var po = BuildApprovedPurchaseOrder();
        poRepo.Add(po);

        var request = new CreateGoodsReceiptNoteRequest(
            po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(Guid.NewGuid(), 5) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_receiving_more_than_the_ordered_quantity_in_one_grn()
    {
        var service = BuildService(out _, out var poRepo, out _);
        var po = BuildApprovedPurchaseOrder(quantity: 100);
        poRepo.Add(po);

        var request = new CreateGoodsReceiptNoteRequest(
            po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(po.Lines.Single().Id, 150) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_cumulative_receipts_across_multiple_grns_exceeding_ordered_quantity()
    {
        var service = BuildService(out _, out var poRepo, out _);
        var po = BuildApprovedPurchaseOrder(quantity: 100);
        poRepo.Add(po);
        var poLineId = po.Lines.Single().Id;

        var first = await service.CreateAsync(
            new CreateGoodsReceiptNoteRequest(po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(poLineId, 70) }),
            "ahmer.bilal", "C001");
        Assert.Equal(70, first.ReceivedValue / 10);

        var second = new CreateGoodsReceiptNoteRequest(
            po.Id, new DateOnly(2026, 7, 21), new[] { new CreateGoodsReceiptNoteLineRequest(poLineId, 40) });
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(second, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task A_rejected_grn_does_not_count_toward_the_ordered_quantity_limit()
    {
        var service = BuildService(out _, out var poRepo, out _);
        var po = BuildApprovedPurchaseOrder(quantity: 100);
        poRepo.Add(po);
        var poLineId = po.Lines.Single().Id;

        var first = await service.CreateAsync(
            new CreateGoodsReceiptNoteRequest(po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(poLineId, 80) }),
            "ahmer.bilal", "C001");
        await service.SubmitAsync(first.Id, "ahmer.bilal");
        await service.RejectAsync(first.Id, "procurement.manager");

        var second = await service.CreateAsync(
            new CreateGoodsReceiptNoteRequest(po.Id, new DateOnly(2026, 7, 21), new[] { new CreateGoodsReceiptNoteLineRequest(poLineId, 80) }),
            "ahmer.bilal", "C001");
        Assert.Equal("Draft", second.Status);
    }

    [Fact]
    public async Task Full_lifecycle_draft_to_submitted_to_approved()
    {
        var service = BuildService(out _, out var poRepo, out _);
        var po = BuildApprovedPurchaseOrder();
        poRepo.Add(po);

        var created = await service.CreateAsync(
            new CreateGoodsReceiptNoteRequest(po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(po.Lines.Single().Id, 10) }),
            "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);

        var approved = await service.ApproveAsync(created.Id, "procurement.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var poRepo, out var auditLog);
        var po = BuildApprovedPurchaseOrder();
        poRepo.Add(po);

        var created = await service.CreateAsync(
            new CreateGoodsReceiptNoteRequest(po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(po.Lines.Single().Id, 10) }),
            "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "GoodsReceiptNote", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out var poRepo, out _);
        var po = BuildApprovedPurchaseOrder();
        poRepo.Add(po);

        var request = new CreateGoodsReceiptNoteRequest(
            po.Id, new DateOnly(2026, 7, 20), new[] { new CreateGoodsReceiptNoteLineRequest(po.Lines.Single().Id, 10) });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(request, "procurement.manager", "C001"));
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _, out _, out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
