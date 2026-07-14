using Modules.MasterData.Domain;
using Platform.Core;

namespace Modules.MasterData.Tests;

public class ItemTests
{
    [Fact]
    public void A_new_item_starts_in_draft_with_no_document_number()
    {
        var item = new Item("ahmer.bilal", "MAT-1010", "Portland Cement 42.5N", ItemType.Stock, "TON");

        Assert.Equal(BusinessObjectStatus.Draft, item.Status);
        Assert.Null(item.DocumentNumber);
        Assert.Equal("MAT-1010", item.ItemCode);
        Assert.Equal("Portland Cement 42.5N", item.ItemName);
        Assert.Equal(ItemType.Stock, item.ItemType);
        Assert.Equal("TON", item.UnitOfMeasure);
        Assert.True(item.IsActive);
    }

    [Fact]
    public void Blank_item_code_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Item("ahmer.bilal", "  ", "Name", ItemType.Stock, "EA"));
    }

    [Fact]
    public void Blank_item_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Item("ahmer.bilal", "MAT-1010", "", ItemType.Stock, "EA"));
    }

    [Fact]
    public void Blank_unit_of_measure_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Item("ahmer.bilal", "MAT-1010", "Cement", ItemType.Stock, " "));
    }

    [Fact]
    public void Submit_then_approve_reaches_approved_the_same_way_every_BO_does()
    {
        var item = new Item("ahmer.bilal", "SVC-2001", "Formwork Subcontract Labor", ItemType.Service, "HR");
        item.AssignNumber("MD-ITM-2026-000001");
        item.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, item.Status);
        item.Approve("finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, item.Status);
    }

    [Fact]
    public void Deactivate_marks_inactive_and_can_be_reactivated()
    {
        var item = new Item("ahmer.bilal", "MAT-1010", "Cement", ItemType.Stock, "TON");
        Assert.True(item.IsActive);
        item.Deactivate();
        Assert.False(item.IsActive);
        item.Activate();
        Assert.True(item.IsActive);
    }

    [Fact]
    public void Arabic_name_is_null_until_set_and_can_be_set_independently()
    {
        var item = new Item("ahmer.bilal", "MAT-1010", "Portland Cement", ItemType.Stock, "TON");
        Assert.Null(item.ItemNameArabic);

        item.UpdateItemNameArabic("أسمنت بورتلاندي");
        Assert.Equal("أسمنت بورتلاندي", item.ItemNameArabic);
    }

    [Fact]
    public void UpdateUnitOfMeasure_rejects_blank()
    {
        var item = new Item("ahmer.bilal", "MAT-1010", "Cement", ItemType.Stock, "TON");
        Assert.Throws<ArgumentException>(() => item.UpdateUnitOfMeasure(""));
    }
}
