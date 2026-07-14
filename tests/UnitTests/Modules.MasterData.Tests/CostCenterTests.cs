using Modules.MasterData.Domain;
using Platform.Core;

namespace Modules.MasterData.Tests;

public class CostCenterTests
{
    [Fact]
    public void A_new_cost_center_starts_in_draft_with_no_document_number()
    {
        var costCenter = new CostCenter("ahmer.bilal", "CC-1000", "Head Office");

        Assert.Equal(BusinessObjectStatus.Draft, costCenter.Status);
        Assert.Null(costCenter.DocumentNumber);
        Assert.Equal("CC-1000", costCenter.CostCenterCode);
        Assert.Equal("Head Office", costCenter.CostCenterName);
        Assert.True(costCenter.IsPostable);
        Assert.True(costCenter.IsActive);
        Assert.Null(costCenter.ParentCostCenterId);
    }

    [Fact]
    public void Blank_cost_center_code_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new CostCenter("ahmer.bilal", "  ", "Name"));
    }

    [Fact]
    public void Blank_cost_center_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new CostCenter("ahmer.bilal", "CC-1000", ""));
    }

    [Fact]
    public void Submit_then_approve_reaches_approved_the_same_way_every_BO_does()
    {
        var costCenter = new CostCenter("ahmer.bilal", "CC-1000", "Head Office");
        costCenter.AssignNumber("MD-CC-2026-000001");
        costCenter.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, costCenter.Status);
        costCenter.Approve("finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, costCenter.Status);
    }

    [Fact]
    public void Deactivate_marks_inactive_and_can_be_reactivated()
    {
        var costCenter = new CostCenter("ahmer.bilal", "CC-1000", "Head Office");
        Assert.True(costCenter.IsActive);
        costCenter.Deactivate();
        Assert.False(costCenter.IsActive);
        costCenter.Activate();
        Assert.True(costCenter.IsActive);
    }

    [Fact]
    public void SetPostable_toggles_header_vs_leaf()
    {
        var costCenter = new CostCenter("ahmer.bilal", "CC-1000", "Head Office");
        costCenter.SetPostable(false);
        Assert.False(costCenter.IsPostable);
    }

    [Fact]
    public void AssignParent_sets_the_hierarchy_link()
    {
        var parentId = Guid.NewGuid();
        var costCenter = new CostCenter("ahmer.bilal", "CC-1010", "Finance Department");
        costCenter.AssignParent(parentId);
        Assert.Equal(parentId, costCenter.ParentCostCenterId);
    }

    [Fact]
    public void Arabic_name_is_null_until_set_and_can_be_set_independently()
    {
        var costCenter = new CostCenter("ahmer.bilal", "CC-1000", "Head Office");
        Assert.Null(costCenter.CostCenterNameArabic);

        costCenter.UpdateCostCenterNameArabic("المكتب الرئيسي");
        Assert.Equal("المكتب الرئيسي", costCenter.CostCenterNameArabic);
    }
}
