using Modules.Procurement.Domain;
using Platform.Core;

namespace Modules.Procurement.Tests;

public class PurchaseOrderTests
{
    private static readonly Guid VendorId = Guid.NewGuid();

    [Fact]
    public void A_new_po_starts_in_draft_with_no_document_number()
    {
        var po = new PurchaseOrder("ahmer.bilal", VendorId);

        Assert.Equal(BusinessObjectStatus.Draft, po.Status);
        Assert.Null(po.DocumentNumber);
        Assert.Null(po.RequestForQuotationId);
        Assert.Empty(po.Lines);
        Assert.Equal(0, po.Total);
    }

    [Fact]
    public void AddLine_rejects_zero_or_negative_quantity_and_price()
    {
        var po = new PurchaseOrder("ahmer.bilal", VendorId);

        Assert.Throws<ArgumentException>(() => po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 0, 10));
        Assert.Throws<ArgumentException>(() => po.AddLine(Guid.NewGuid(), Guid.NewGuid(), -1, 10));
        Assert.Throws<ArgumentException>(() => po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, 0));
        Assert.Throws<ArgumentException>(() => po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, -5));
    }

    [Fact]
    public void Total_sums_line_totals()
    {
        var po = new PurchaseOrder("ahmer.bilal", VendorId);
        po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
        po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 2, 100);

        Assert.Equal(250, po.Total);
    }

    [Fact]
    public void AddLine_after_submit_is_rejected()
    {
        var po = new PurchaseOrder("ahmer.bilal", VendorId);
        po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
        po.AssignNumber("PROC-PO-2026-000001");
        po.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, 1));
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var po = new PurchaseOrder("ahmer.bilal", VendorId);
        po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
        po.AssignNumber("PROC-PO-2026-000001");

        po.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, po.Status);

        po.Approve("procurement.manager");
        Assert.Equal(BusinessObjectStatus.Approved, po.Status);
    }

    [Fact]
    public void Records_the_source_rfq_line_id_for_traceability_when_provided()
    {
        var po = new PurchaseOrder("ahmer.bilal", VendorId, Guid.NewGuid());
        var rfqLineId = Guid.NewGuid();
        var line = po.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, 1, rfqLineId);

        Assert.Equal(rfqLineId, line.RfqLineId);
        Assert.NotNull(po.RequestForQuotationId);
    }
}
