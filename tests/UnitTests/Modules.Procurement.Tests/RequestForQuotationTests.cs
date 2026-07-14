using Modules.Procurement.Domain;
using Platform.Core;

namespace Modules.Procurement.Tests;

public class RequestForQuotationTests
{
    private static readonly Guid PurchaseRequisitionId = Guid.NewGuid();

    [Fact]
    public void A_new_rfq_starts_in_draft_with_no_document_number()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Cement quotes for Tower A");

        Assert.Equal(BusinessObjectStatus.Draft, rfq.Status);
        Assert.Null(rfq.DocumentNumber);
        Assert.Empty(rfq.Lines);
        Assert.Empty(rfq.InvitedVendors);
        Assert.Empty(rfq.VendorQuoteLines);
    }

    [Fact]
    public void Blank_description_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, ""));
    }

    [Fact]
    public void InviteVendor_rejects_the_same_vendor_twice()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test");
        var vendorId = Guid.NewGuid();
        rfq.InviteVendor(vendorId);

        Assert.Throws<ArgumentException>(() => rfq.InviteVendor(vendorId));
    }

    [Fact]
    public void AddLine_and_InviteVendor_after_submit_are_rejected()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test");
        rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10);
        rfq.InviteVendor(Guid.NewGuid());
        rfq.AssignNumber("PROC-RFQ-2026-000001");
        rfq.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1));
        Assert.Throws<InvalidOperationException>(() => rfq.InviteVendor(Guid.NewGuid()));
    }

    [Fact]
    public void RecordVendorQuote_before_submit_is_rejected()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test");
        var line = rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10);
        var vendorId = Guid.NewGuid();
        rfq.InviteVendor(vendorId);

        Assert.Throws<InvalidOperationException>(() => rfq.RecordVendorQuote(vendorId, line.Id, 100));
    }

    [Fact]
    public void RecordVendorQuote_rejects_a_vendor_that_was_not_invited()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test");
        var line = rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10);
        rfq.InviteVendor(Guid.NewGuid());
        rfq.AssignNumber("PROC-RFQ-2026-000001");
        rfq.Submit("ahmer.bilal");

        Assert.Throws<ArgumentException>(() => rfq.RecordVendorQuote(Guid.NewGuid(), line.Id, 100));
    }

    [Fact]
    public void RecordVendorQuote_rejects_a_line_that_does_not_belong_to_this_rfq()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test");
        var vendorId = Guid.NewGuid();
        rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10);
        rfq.InviteVendor(vendorId);
        rfq.AssignNumber("PROC-RFQ-2026-000001");
        rfq.Submit("ahmer.bilal");

        Assert.Throws<ArgumentException>(() => rfq.RecordVendorQuote(vendorId, Guid.NewGuid(), 100));
    }

    [Fact]
    public void RecordVendorQuote_rejects_zero_or_negative_price()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test");
        var vendorId = Guid.NewGuid();
        var line = rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10);
        rfq.InviteVendor(vendorId);
        rfq.AssignNumber("PROC-RFQ-2026-000001");
        rfq.Submit("ahmer.bilal");

        Assert.Throws<ArgumentException>(() => rfq.RecordVendorQuote(vendorId, line.Id, 0));
        Assert.Throws<ArgumentException>(() => rfq.RecordVendorQuote(vendorId, line.Id, -5));
    }

    [Fact]
    public void RecordVendorQuote_rejects_the_same_vendor_and_line_twice()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test");
        var vendorId = Guid.NewGuid();
        var line = rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10);
        rfq.InviteVendor(vendorId);
        rfq.AssignNumber("PROC-RFQ-2026-000001");
        rfq.Submit("ahmer.bilal");
        rfq.RecordVendorQuote(vendorId, line.Id, 100);

        Assert.Throws<ArgumentException>(() => rfq.RecordVendorQuote(vendorId, line.Id, 90));
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved_with_a_recorded_quote()
    {
        var rfq = new RequestForQuotation("ahmer.bilal", PurchaseRequisitionId, "Test", new DateOnly(2026, 8, 1));
        var vendorId = Guid.NewGuid();
        var line = rfq.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10);
        rfq.InviteVendor(vendorId);
        rfq.AssignNumber("PROC-RFQ-2026-000001");

        rfq.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, rfq.Status);

        rfq.RecordVendorQuote(vendorId, line.Id, 27.50m);
        Assert.Single(rfq.VendorQuoteLines);

        rfq.Approve("procurement.manager");
        Assert.Equal(BusinessObjectStatus.Approved, rfq.Status);
    }
}
