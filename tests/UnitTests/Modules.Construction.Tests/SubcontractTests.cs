using Modules.Construction.Domain;
using Platform.Core;

namespace Modules.Construction.Tests;

public class SubcontractTests
{
    private static readonly Guid WbsElementId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid SubcontractorId = Guid.NewGuid();

    private static Subcontract NewSubcontract(
        Guid? contractId = null, decimal? retention = null, decimal? mobilizationAdvance = null, int? defectsLiabilityMonths = null) =>
        new("ahmer.bilal", ProjectId, contractId, SubcontractorId, retention, mobilizationAdvance, defectsLiabilityMonths);

    [Fact]
    public void A_new_subcontract_starts_in_draft_with_no_document_number()
    {
        var subcontract = NewSubcontract();

        Assert.Equal(BusinessObjectStatus.Draft, subcontract.Status);
        Assert.Null(subcontract.DocumentNumber);
        Assert.Empty(subcontract.Lines);
        Assert.Empty(subcontract.BackCharges);
        Assert.Equal(0m, subcontract.SubcontractValue);
        Assert.Equal(0m, subcontract.TotalBackCharges);
        Assert.Equal(0m, subcontract.NetPayableValue);
    }

    [Fact]
    public void Retention_percentage_outside_0_to_100_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => NewSubcontract(retention: 150m));
        Assert.Throws<ArgumentException>(() => NewSubcontract(retention: -1m));
    }

    [Fact]
    public void Mobilization_advance_percentage_outside_0_to_100_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => NewSubcontract(mobilizationAdvance: 150m));
        Assert.Throws<ArgumentException>(() => NewSubcontract(mobilizationAdvance: -1m));
    }

    [Fact]
    public void Negative_defects_liability_period_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => NewSubcontract(defectsLiabilityMonths: -1));
    }

    [Fact]
    public void AddLine_computes_amount_and_rolls_up_subcontract_value()
    {
        var subcontract = NewSubcontract();
        var line = subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, WbsElementId);

        Assert.Equal(5000m, line.Amount);
        Assert.Equal(5000m, subcontract.SubcontractValue);
        Assert.Equal(5000m, subcontract.NetPayableValue);
    }

    [Fact]
    public void AddLine_rejects_a_duplicate_code_within_the_same_subcontract()
    {
        var subcontract = NewSubcontract();
        subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, WbsElementId);
        Assert.Throws<ArgumentException>(() => subcontract.AddLine("SUB-001", "Duplicate", null, "M2", 10m, 5m, WbsElementId));
    }

    [Fact]
    public void AddLine_rejects_zero_or_negative_quantity_or_rate()
    {
        var subcontract = NewSubcontract();
        Assert.Throws<ArgumentException>(() => subcontract.AddLine("SUB-001", "Formwork", null, "M2", 0m, 50m, WbsElementId));
        Assert.Throws<ArgumentException>(() => subcontract.AddLine("SUB-002", "Formwork", null, "M2", 100m, -1m, WbsElementId));
    }

    [Fact]
    public void AddLine_after_submit_is_rejected()
    {
        var subcontract = NewSubcontract();
        subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, WbsElementId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => subcontract.AddLine("SUB-002", "Rebar", null, "TON", 10m, 5m, WbsElementId));
    }

    [Fact]
    public void AddBackCharge_before_approved_is_rejected()
    {
        var subcontract = NewSubcontract();
        subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, WbsElementId);

        Assert.Throws<InvalidOperationException>(
            () => subcontract.AddBackCharge("Rework of defective formwork", 500m, new DateOnly(2026, 7, 16)));
    }

    [Fact]
    public void AddBackCharge_rejects_zero_or_negative_amount()
    {
        var subcontract = NewSubcontract();
        subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, WbsElementId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");
        subcontract.Approve("con.manager");

        Assert.Throws<ArgumentException>(() => subcontract.AddBackCharge("Bad charge", 0m, new DateOnly(2026, 7, 16)));
        Assert.Throws<ArgumentException>(() => subcontract.AddBackCharge("Bad charge", -10m, new DateOnly(2026, 7, 16)));
    }

    [Fact]
    public void AddBackCharge_after_approved_reduces_net_payable_value()
    {
        var subcontract = NewSubcontract();
        subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, WbsElementId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");
        subcontract.Approve("con.manager");

        var backCharge = subcontract.AddBackCharge("Rework of defective formwork", 500m, new DateOnly(2026, 7, 16));

        Assert.Equal(500m, backCharge.Amount);
        Assert.Equal(5000m, subcontract.SubcontractValue);
        Assert.Equal(500m, subcontract.TotalBackCharges);
        Assert.Equal(4500m, subcontract.NetPayableValue);
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var subcontract = NewSubcontract();
        subcontract.AddLine("SUB-001", "Formwork", null, "M2", 100m, 50m, WbsElementId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");

        subcontract.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, subcontract.Status);

        subcontract.Approve("con.manager");
        Assert.Equal(BusinessObjectStatus.Approved, subcontract.Status);
    }
}
