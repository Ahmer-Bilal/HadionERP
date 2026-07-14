using Modules.Procurement.Domain;
using Platform.Core;

namespace Modules.Procurement.Tests;

public class VendorPrequalificationTests
{
    private static readonly Guid VendorId = Guid.NewGuid();

    [Fact]
    public void A_new_prequalification_starts_in_draft_with_no_validity_period()
    {
        var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Supplier");

        Assert.Equal(BusinessObjectStatus.Draft, prequalification.Status);
        Assert.Null(prequalification.DocumentNumber);
        Assert.Equal(VendorId, prequalification.BusinessPartnerId);
        Assert.Equal("Supplier", prequalification.RoleType);
        Assert.Null(prequalification.Trade);
        Assert.Null(prequalification.ValidFrom);
        Assert.Null(prequalification.ValidUntil);
    }

    [Fact]
    public void Trade_can_be_carried_alongside_a_role_type()
    {
        var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Subcontractor", "Electrical");
        Assert.Equal("Electrical", prequalification.Trade);
    }

    [Fact]
    public void Blank_role_type_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new VendorPrequalification("ahmer.bilal", VendorId, "  "));
    }

    [Fact]
    public void Submit_then_approve_reaches_approved_the_same_way_every_BO_does()
    {
        var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Supplier");
        prequalification.AssignNumber("PROC-VPQ-2026-000001");
        prequalification.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, prequalification.Status);
        prequalification.Approve("procurement.reviewer");
        Assert.Equal(BusinessObjectStatus.Approved, prequalification.Status);
    }

    [Fact]
    public void SetValidityPeriod_computes_valid_until_from_valid_from_plus_months()
    {
        var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Supplier");
        var validFrom = new DateOnly(2026, 7, 14);

        prequalification.SetValidityPeriod(validFrom, 24);

        Assert.Equal(validFrom, prequalification.ValidFrom);
        Assert.Equal(new DateOnly(2028, 7, 14), prequalification.ValidUntil);
    }

    [Fact]
    public void SetValidityPeriod_rejects_zero_or_negative_months()
    {
        var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Supplier");
        Assert.Throws<ArgumentOutOfRangeException>(() => prequalification.SetValidityPeriod(new DateOnly(2026, 7, 14), 0));
    }

    [Fact]
    public void SetValidityPeriod_cannot_be_called_twice()
    {
        var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Supplier");
        prequalification.SetValidityPeriod(new DateOnly(2026, 7, 14), 24);

        Assert.Throws<InvalidOperationException>(() => prequalification.SetValidityPeriod(new DateOnly(2026, 7, 14), 12));
    }

    [Fact]
    public void IsValidAsOf_is_false_before_approval()
    {
        var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Supplier");
        Assert.False(prequalification.IsValidAsOf(new DateOnly(2026, 7, 14)));
    }

    [Fact]
    public void IsValidAsOf_is_true_within_the_validity_window_once_approved()
    {
        var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Supplier");
        prequalification.AssignNumber("PROC-VPQ-2026-000001");
        prequalification.Submit("ahmer.bilal");
        prequalification.Approve("procurement.reviewer");
        prequalification.SetValidityPeriod(new DateOnly(2026, 7, 14), 24);

        Assert.True(prequalification.IsValidAsOf(new DateOnly(2026, 7, 14)));
        Assert.True(prequalification.IsValidAsOf(new DateOnly(2028, 7, 14)));
        Assert.False(prequalification.IsValidAsOf(new DateOnly(2026, 7, 13)));
        Assert.False(prequalification.IsValidAsOf(new DateOnly(2028, 7, 15)));
    }
}
