using Modules.Procurement.Domain;
using Platform.Core;

namespace Modules.Procurement.Tests;

public class PurchaseRequisitionTests
{
    [Fact]
    public void A_new_requisition_starts_in_draft_with_no_document_number_and_no_lines()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Rebar for Tower A");

        Assert.Equal(BusinessObjectStatus.Draft, requisition.Status);
        Assert.Null(requisition.DocumentNumber);
        Assert.Empty(requisition.Lines);
        Assert.Equal(0, requisition.EstimatedTotal);
    }

    [Fact]
    public void Blank_description_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new PurchaseRequisition("ahmer.bilal", ""));
    }

    [Fact]
    public void AddLine_rejects_zero_or_negative_quantity()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Test");
        Assert.Throws<ArgumentException>(() => requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 0, 100));
        Assert.Throws<ArgumentException>(() => requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), -5, 100));
    }

    [Fact]
    public void AddLine_rejects_zero_or_negative_estimated_unit_price()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Test");
        Assert.Throws<ArgumentException>(() => requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 0));
        Assert.Throws<ArgumentException>(() => requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, -1));
    }

    [Fact]
    public void EstimatedTotal_sums_quantity_times_unit_price_across_lines()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Test");
        requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 50);
        requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 4, 25);

        Assert.Equal(600, requisition.EstimatedTotal);
    }

    [Fact]
    public void AddLine_after_submit_is_rejected()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Test");
        requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 50);
        requisition.AssignNumber("PROC-PR-2026-000001");
        requisition.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 1, 1));
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var requisition = new PurchaseRequisition("ahmer.bilal", "Test", new DateOnly(2026, 8, 1));
        requisition.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, 50);
        requisition.AssignNumber("PROC-PR-2026-000001");

        requisition.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, requisition.Status);

        requisition.Approve("procurement.manager");
        Assert.Equal(BusinessObjectStatus.Approved, requisition.Status);
        Assert.Equal(new DateOnly(2026, 8, 1), requisition.RequiredByDate);
    }
}
