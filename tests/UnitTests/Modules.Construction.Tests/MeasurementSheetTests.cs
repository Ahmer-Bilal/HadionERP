using Modules.Construction.Domain;
using Platform.Core;

namespace Modules.Construction.Tests;

public class MeasurementSheetTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CommercialDocumentId = Guid.NewGuid();
    private static readonly Guid LineId = Guid.NewGuid();
    private static readonly DateOnly PeriodStart = new(2026, 7, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 7, 31);

    private static MeasurementSheet NewSheet(CommercialDocumentType type = CommercialDocumentType.Contract) =>
        new("ahmer.bilal", ProjectId, type, CommercialDocumentId, PeriodStart, PeriodEnd, notes: null);

    [Fact]
    public void A_new_sheet_starts_in_draft_with_no_document_number()
    {
        var sheet = NewSheet();

        Assert.Equal(BusinessObjectStatus.Draft, sheet.Status);
        Assert.Null(sheet.DocumentNumber);
        Assert.Empty(sheet.Lines);
        Assert.Equal(CommercialDocumentType.Contract, sheet.CommercialDocumentType);
    }

    [Fact]
    public void Period_end_before_period_start_is_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => new MeasurementSheet("ahmer.bilal", ProjectId, CommercialDocumentType.Contract, CommercialDocumentId,
                new DateOnly(2026, 7, 31), new DateOnly(2026, 7, 1), notes: null));
    }

    [Fact]
    public void AddLine_rejects_negative_quantity_submitted()
    {
        var sheet = NewSheet();
        Assert.Throws<ArgumentException>(() => sheet.AddLine(LineId, -1m, remarks: null));
    }

    [Fact]
    public void AddLine_rejects_a_duplicate_commercial_document_line_within_the_same_sheet()
    {
        var sheet = NewSheet();
        sheet.AddLine(LineId, 50m, remarks: null);
        Assert.Throws<ArgumentException>(() => sheet.AddLine(LineId, 10m, remarks: null));
    }

    [Fact]
    public void AddLine_after_submit_is_rejected()
    {
        var sheet = NewSheet();
        sheet.AddLine(LineId, 50m, remarks: null);
        sheet.AssignNumber("CON-MEAS-2026-000001");
        sheet.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => sheet.AddLine(Guid.NewGuid(), 10m, remarks: null));
    }

    [Fact]
    public void RecordCertifiedQuantities_before_submitted_is_rejected()
    {
        var sheet = NewSheet();
        var line = sheet.AddLine(LineId, 50m, remarks: null);

        Assert.Throws<InvalidOperationException>(
            () => sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal> { [line.Id] = 45m }));
    }

    [Fact]
    public void RecordCertifiedQuantities_requires_every_line_covered_exactly_once()
    {
        var sheet = NewSheet();
        var line1 = sheet.AddLine(LineId, 50m, remarks: null);
        var line2 = sheet.AddLine(Guid.NewGuid(), 30m, remarks: null);
        sheet.AssignNumber("CON-MEAS-2026-000001");
        sheet.Submit("ahmer.bilal");

        // Missing line2.
        Assert.Throws<ArgumentException>(
            () => sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal> { [line1.Id] = 45m }));

        // Unknown line id.
        Assert.Throws<ArgumentException>(() => sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal>
        {
            [line1.Id] = 45m, [line2.Id] = 30m, [Guid.NewGuid()] = 5m,
        }));
    }

    [Fact]
    public void RecordCertifiedQuantities_may_certify_lower_than_submitted_per_line()
    {
        var sheet = NewSheet();
        var line = sheet.AddLine(LineId, 50m, remarks: null);
        sheet.AssignNumber("CON-MEAS-2026-000001");
        sheet.Submit("ahmer.bilal");

        sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal> { [line.Id] = 40m });

        Assert.Equal(50m, sheet.Lines.Single().QuantitySubmitted);
        Assert.Equal(40m, sheet.Lines.Single().QuantityCertified);
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var sheet = NewSheet();
        var line = sheet.AddLine(LineId, 50m, remarks: null);
        sheet.AssignNumber("CON-MEAS-2026-000001");

        sheet.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, sheet.Status);

        sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal> { [line.Id] = 50m });
        sheet.Approve("engineer");
        Assert.Equal(BusinessObjectStatus.Approved, sheet.Status);
    }

    [Fact]
    public void Reject_transitions_to_rejected()
    {
        var sheet = NewSheet();
        sheet.AddLine(LineId, 50m, remarks: null);
        sheet.AssignNumber("CON-MEAS-2026-000001");
        sheet.Submit("ahmer.bilal");

        sheet.Reject("engineer");
        Assert.Equal(BusinessObjectStatus.Rejected, sheet.Status);
    }
}
