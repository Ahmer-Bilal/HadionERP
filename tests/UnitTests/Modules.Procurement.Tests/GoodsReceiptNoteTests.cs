using Modules.Procurement.Domain;
using Platform.Core;

namespace Modules.Procurement.Tests;

public class GoodsReceiptNoteTests
{
    private static readonly Guid PurchaseOrderId = Guid.NewGuid();

    [Fact]
    public void A_new_grn_starts_in_draft_with_no_document_number()
    {
        var grn = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 20));

        Assert.Equal(BusinessObjectStatus.Draft, grn.Status);
        Assert.Null(grn.DocumentNumber);
        Assert.Empty(grn.Lines);
        Assert.Equal(0, grn.ReceivedValue);
    }

    [Fact]
    public void AddLine_rejects_zero_or_negative_quantity()
    {
        var grn = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 20));

        Assert.Throws<ArgumentException>(() => grn.AddLine(Guid.NewGuid(), Guid.NewGuid(), 0, 10));
        Assert.Throws<ArgumentException>(() => grn.AddLine(Guid.NewGuid(), Guid.NewGuid(), -1, 10));
    }

    [Fact]
    public void ReceivedValue_sums_line_values_at_the_copied_unit_price()
    {
        var grn = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 20));
        grn.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
        grn.AddLine(Guid.NewGuid(), Guid.NewGuid(), 2, 100);

        Assert.Equal(250, grn.ReceivedValue);
    }

    [Fact]
    public void AddLine_after_submit_is_rejected()
    {
        var grn = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 20));
        grn.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
        grn.AssignNumber("PROC-GRN-2026-000001");
        grn.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => grn.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, 1));
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var grn = new GoodsReceiptNote("ahmer.bilal", PurchaseOrderId, new DateOnly(2026, 7, 20));
        grn.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
        grn.AssignNumber("PROC-GRN-2026-000001");

        grn.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, grn.Status);

        grn.Approve("procurement.manager");
        Assert.Equal(BusinessObjectStatus.Approved, grn.Status);
    }
}
