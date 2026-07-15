using Modules.Finance.Contracts;
using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Tests;

public class ThreeWayMatchServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid VendorId = Guid.NewGuid();
    private static readonly Guid OtherVendorId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid CostCenterId = Guid.NewGuid();

    private static PurchaseOrder BuildApprovedPurchaseOrder(decimal quantity, decimal unitPrice, Guid vendorId, out Guid lineId)
    {
        var po = new PurchaseOrder("ahmer.bilal", vendorId);
        var line = po.AddLine(ItemId, CostCenterId, quantity, unitPrice);
        po.AssignNumber($"PROC-PO-{CurrentYear}-000001");
        po.Submit("ahmer.bilal");
        po.Approve("procurement.manager");
        lineId = line.Id;
        return po;
    }

    private static GoodsReceiptNote BuildApprovedGrn(Guid purchaseOrderId, Guid poLineId, decimal quantity, decimal unitPrice)
    {
        var grn = new GoodsReceiptNote("ahmer.bilal", purchaseOrderId, new DateOnly(2026, 7, 20));
        grn.AddLine(poLineId, ItemId, quantity, unitPrice);
        grn.AssignNumber($"PROC-GRN-{CurrentYear}-000001");
        grn.Submit("ahmer.bilal");
        grn.Approve("procurement.manager");
        return grn;
    }

    private static ThreeWayMatchService BuildService(
        out FakePurchaseOrderRepository poRepo, out FakeGoodsReceiptNoteRepository grnRepo, out FakeAPInvoiceLookup invoiceLookup)
    {
        poRepo = new FakePurchaseOrderRepository();
        grnRepo = new FakeGoodsReceiptNoteRepository();
        invoiceLookup = new FakeAPInvoiceLookup();
        return new ThreeWayMatchService(poRepo, grnRepo, invoiceLookup);
    }

    [Fact]
    public async Task Matches_when_invoiced_amount_is_within_received_and_ordered_and_vendor_agrees()
    {
        var service = BuildService(out var poRepo, out var grnRepo, out var invoiceLookup);
        var po = BuildApprovedPurchaseOrder(100, 10, VendorId, out var lineId);
        poRepo.Add(po);
        var grn = BuildApprovedGrn(po.Id, lineId, 100, 10);
        grnRepo.Add(grn);
        var invoiceId = Guid.NewGuid();
        invoiceLookup.Add(new APInvoiceSummary(invoiceId, "FIN-AP-2026-000001", VendorId, "Approved", 900, 1035));

        var result = await service.CheckAsync(po.Id, invoiceId);

        Assert.True(result.IsMatched);
        Assert.True(result.VendorMatches);
        Assert.True(result.WithinReceived);
        Assert.True(result.WithinOrdered);
        Assert.Equal(1000, result.OrderedTotal);
        Assert.Equal(1000, result.ReceivedValue);
        Assert.Equal(900, result.InvoicedNetAmount);
        Assert.Empty(result.VarianceNotes);
    }

    [Fact]
    public async Task Flags_a_vendor_mismatch()
    {
        var service = BuildService(out var poRepo, out var grnRepo, out var invoiceLookup);
        var po = BuildApprovedPurchaseOrder(100, 10, VendorId, out var lineId);
        poRepo.Add(po);
        grnRepo.Add(BuildApprovedGrn(po.Id, lineId, 100, 10));
        var invoiceId = Guid.NewGuid();
        invoiceLookup.Add(new APInvoiceSummary(invoiceId, "FIN-AP-2026-000002", OtherVendorId, "Approved", 500, 575));

        var result = await service.CheckAsync(po.Id, invoiceId);

        Assert.False(result.IsMatched);
        Assert.False(result.VendorMatches);
        Assert.NotEmpty(result.VarianceNotes);
    }

    [Fact]
    public async Task Flags_invoiced_amount_exceeding_received_value()
    {
        var service = BuildService(out var poRepo, out var grnRepo, out var invoiceLookup);
        var po = BuildApprovedPurchaseOrder(100, 10, VendorId, out var lineId);
        poRepo.Add(po);
        grnRepo.Add(BuildApprovedGrn(po.Id, lineId, 40, 10)); // only 400 received
        var invoiceId = Guid.NewGuid();
        invoiceLookup.Add(new APInvoiceSummary(invoiceId, "FIN-AP-2026-000003", VendorId, "Approved", 900, 1035));

        var result = await service.CheckAsync(po.Id, invoiceId);

        Assert.False(result.IsMatched);
        Assert.False(result.WithinReceived);
        Assert.True(result.WithinOrdered);
        Assert.Equal(400, result.ReceivedValue);
    }

    [Fact]
    public async Task Flags_invoiced_amount_exceeding_the_purchase_order_total()
    {
        var service = BuildService(out var poRepo, out var grnRepo, out var invoiceLookup);
        var po = BuildApprovedPurchaseOrder(10, 10, VendorId, out var lineId); // ordered total 100
        poRepo.Add(po);
        grnRepo.Add(BuildApprovedGrn(po.Id, lineId, 10, 10));
        var invoiceId = Guid.NewGuid();
        invoiceLookup.Add(new APInvoiceSummary(invoiceId, "FIN-AP-2026-000004", VendorId, "Approved", 150, 172.5m));

        var result = await service.CheckAsync(po.Id, invoiceId);

        Assert.False(result.IsMatched);
        Assert.False(result.WithinOrdered);
    }

    [Fact]
    public async Task Unapproved_grns_do_not_count_toward_the_received_value()
    {
        var service = BuildService(out var poRepo, out var grnRepo, out var invoiceLookup);
        var po = BuildApprovedPurchaseOrder(100, 10, VendorId, out var lineId);
        poRepo.Add(po);
        var draftGrn = new GoodsReceiptNote("ahmer.bilal", po.Id, new DateOnly(2026, 7, 20));
        draftGrn.AddLine(lineId, ItemId, 100, 10);
        draftGrn.AssignNumber($"PROC-GRN-{CurrentYear}-000002");
        grnRepo.Add(draftGrn); // still Draft, not Approved
        var invoiceId = Guid.NewGuid();
        invoiceLookup.Add(new APInvoiceSummary(invoiceId, "FIN-AP-2026-000005", VendorId, "Approved", 500, 575));

        var result = await service.CheckAsync(po.Id, invoiceId);

        Assert.Equal(0, result.ReceivedValue);
        Assert.False(result.WithinReceived);
    }

    [Fact]
    public async Task CheckAsync_throws_for_an_unknown_purchase_order()
    {
        var service = BuildService(out _, out _, out var invoiceLookup);
        invoiceLookup.Add(new APInvoiceSummary(Guid.NewGuid(), "FIN-AP-2026-000006", VendorId, "Approved", 100, 115));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CheckAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CheckAsync_throws_for_an_unknown_invoice()
    {
        var service = BuildService(out var poRepo, out _, out _);
        var po = BuildApprovedPurchaseOrder(10, 10, VendorId, out _);
        poRepo.Add(po);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CheckAsync(po.Id, Guid.NewGuid()));
    }
}
