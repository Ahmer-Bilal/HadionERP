using Modules.Construction.Domain;
using Platform.Core;

namespace Modules.Construction.Tests;

public class IpcTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CommercialDocumentId = Guid.NewGuid();
    private static readonly Guid MeasurementSheetId = Guid.NewGuid();
    private static readonly Guid LineId = Guid.NewGuid();
    private static readonly DateOnly PeriodStart = new(2026, 7, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 7, 31);

    private static Ipc NewIpc(decimal? retention = 10m, decimal? advance = 15m, decimal otherDeductions = 0m) =>
        new("ahmer.bilal", ProjectId, CommercialDocumentType.Contract, CommercialDocumentId, MeasurementSheetId,
            PeriodStart, PeriodEnd, retention, advance, otherDeductions);

    [Fact]
    public void A_new_ipc_starts_in_draft_with_no_document_number_and_zero_values()
    {
        var ipc = NewIpc();

        Assert.Equal(BusinessObjectStatus.Draft, ipc.Status);
        Assert.Null(ipc.DocumentNumber);
        Assert.Empty(ipc.Lines);
        Assert.Equal(0m, ipc.GrossValueToDate);
        Assert.Equal(0m, ipc.NetPayable);
    }

    [Fact]
    public void Period_end_before_period_start_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Ipc(
            "ahmer.bilal", ProjectId, CommercialDocumentType.Contract, CommercialDocumentId, MeasurementSheetId,
            new DateOnly(2026, 7, 31), new DateOnly(2026, 7, 1), 10m, 15m, 0m));
    }

    [Fact]
    public void Retention_and_advance_percentages_outside_0_to_100_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => NewIpc(retention: 150m));
        Assert.Throws<ArgumentException>(() => NewIpc(retention: -1m));
        Assert.Throws<ArgumentException>(() => NewIpc(advance: 150m));
        Assert.Throws<ArgumentException>(() => NewIpc(advance: -1m));
    }

    [Fact]
    public void Negative_other_deductions_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => NewIpc(otherDeductions: -1m));
    }

    [Fact]
    public void AddLine_rejects_a_rate_of_zero_or_negative()
    {
        var ipc = NewIpc();
        Assert.Throws<ArgumentException>(() => ipc.AddLine(LineId, 0m, 40m, 40m));
        Assert.Throws<ArgumentException>(() => ipc.AddLine(LineId, -1m, 40m, 40m));
    }

    [Fact]
    public void AddLine_rejects_quantity_to_date_less_than_quantity_this_period()
    {
        var ipc = NewIpc();
        Assert.Throws<ArgumentException>(() => ipc.AddLine(LineId, 50m, 40m, 30m));
    }

    [Fact]
    public void AddLine_rejects_a_duplicate_commercial_document_line_within_the_same_ipc()
    {
        var ipc = NewIpc();
        ipc.AddLine(LineId, 50m, 40m, 40m);
        Assert.Throws<ArgumentException>(() => ipc.AddLine(LineId, 50m, 10m, 50m));
    }

    [Fact]
    public void AddLine_after_submit_is_rejected()
    {
        var ipc = NewIpc();
        ipc.AddLine(LineId, 50m, 40m, 40m);
        ipc.AssignNumber("CON-IPC-2026-000001");
        ipc.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => ipc.AddLine(Guid.NewGuid(), 10m, 5m, 5m));
    }

    [Fact]
    public void The_waterfall_computes_correctly_from_lines_and_header_percentages()
    {
        // Line: rate 50, this period 40, to-date 100 (i.e. 60 was already certified in a prior IPC).
        var ipc = NewIpc(retention: 10m, advance: 15m, otherDeductions: 50m);
        ipc.AddLine(LineId, 50m, 40m, 100m);

        Assert.Equal(5000m, ipc.GrossValueToDate); // 100 * 50
        Assert.Equal(2000m, ipc.GrossValueThisPeriod); // 40 * 50
        Assert.Equal(3000m, ipc.GrossValuePreviousIpc); // 5000 - 2000
        Assert.Equal(200m, ipc.RetentionAmount); // 2000 * 10%
        Assert.Equal(300m, ipc.AdvanceRecoveryAmount); // 2000 * 15%
        // Net = 2000 - 200 - 300 - 50 (other deductions) = 1450
        Assert.Equal(1450m, ipc.NetPayable);
    }

    [Fact]
    public void Null_retention_and_advance_percentages_default_the_deduction_to_zero()
    {
        var ipc = NewIpc(retention: null, advance: null);
        ipc.AddLine(LineId, 50m, 40m, 40m);

        Assert.Equal(0m, ipc.RetentionAmount);
        Assert.Equal(0m, ipc.AdvanceRecoveryAmount);
        Assert.Equal(2000m, ipc.NetPayable);
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var ipc = NewIpc();
        ipc.AddLine(LineId, 50m, 40m, 40m);
        ipc.AssignNumber("CON-IPC-2026-000001");

        ipc.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, ipc.Status);

        ipc.Approve("engineer");
        Assert.Equal(BusinessObjectStatus.Approved, ipc.Status);
    }

    [Fact]
    public void Reject_transitions_to_rejected()
    {
        var ipc = NewIpc();
        ipc.AddLine(LineId, 50m, 40m, 40m);
        ipc.AssignNumber("CON-IPC-2026-000001");
        ipc.Submit("ahmer.bilal");

        ipc.Reject("engineer");
        Assert.Equal(BusinessObjectStatus.Rejected, ipc.Status);
    }
}
