using Modules.MasterData.Domain;
using Platform.Core;

namespace Modules.MasterData.Tests;

public class TaxCodeTests
{
    [Fact]
    public void A_new_tax_code_starts_in_draft_with_no_document_number()
    {
        var taxCode = new TaxCode("ahmer.bilal", "VAT15", "Standard VAT 15%", 15.00m, TaxType.Standard);

        Assert.Equal(BusinessObjectStatus.Draft, taxCode.Status);
        Assert.Null(taxCode.DocumentNumber);
        Assert.Equal("VAT15", taxCode.TaxCodeCode);
        Assert.Equal("Standard VAT 15%", taxCode.TaxCodeName);
        Assert.Equal(15.00m, taxCode.Rate);
        Assert.Equal(TaxType.Standard, taxCode.TaxType);
        Assert.True(taxCode.IsActive);
    }

    [Fact]
    public void Blank_tax_code_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new TaxCode("ahmer.bilal", "  ", "Name", 15m, TaxType.Standard));
    }

    [Fact]
    public void Blank_tax_code_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new TaxCode("ahmer.bilal", "VAT15", "", 15m, TaxType.Standard));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void Rate_outside_0_to_100_is_rejected(double rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TaxCode("ahmer.bilal", "VAT15", "Name", (decimal)rate, TaxType.Standard));
    }

    [Fact]
    public void Zero_rate_is_allowed_for_zero_rated_type()
    {
        var taxCode = new TaxCode("ahmer.bilal", "ZERO", "Zero-Rated", 0m, TaxType.ZeroRated);
        Assert.Equal(0m, taxCode.Rate);
    }

    [Fact]
    public void UpdateRate_rejects_out_of_range()
    {
        var taxCode = new TaxCode("ahmer.bilal", "VAT15", "Name", 15m, TaxType.Standard);
        Assert.Throws<ArgumentOutOfRangeException>(() => taxCode.UpdateRate(-5));
    }

    [Fact]
    public void Submit_then_approve_reaches_approved_the_same_way_every_BO_does()
    {
        var taxCode = new TaxCode("ahmer.bilal", "VAT15", "Standard VAT 15%", 15m, TaxType.Standard);
        taxCode.AssignNumber("MD-TAX-2026-000001");
        taxCode.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, taxCode.Status);
        taxCode.Approve("finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, taxCode.Status);
    }

    [Fact]
    public void Deactivate_marks_inactive_and_can_be_reactivated()
    {
        var taxCode = new TaxCode("ahmer.bilal", "VAT15", "Standard VAT 15%", 15m, TaxType.Standard);
        Assert.True(taxCode.IsActive);
        taxCode.Deactivate();
        Assert.False(taxCode.IsActive);
        taxCode.Activate();
        Assert.True(taxCode.IsActive);
    }

    [Fact]
    public void Arabic_name_is_null_until_set_and_can_be_set_independently()
    {
        var taxCode = new TaxCode("ahmer.bilal", "VAT15", "Standard VAT 15%", 15m, TaxType.Standard);
        Assert.Null(taxCode.TaxCodeNameArabic);

        taxCode.UpdateTaxCodeNameArabic("ضريبة القيمة المضافة 15%");
        Assert.Equal("ضريبة القيمة المضافة 15%", taxCode.TaxCodeNameArabic);
    }
}
