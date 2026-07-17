namespace Modules.Construction.Domain;

/// <summary>
/// One line of a <see cref="MeasurementSheet"/> — measured progress against a single line of the sheet's
/// own commercial document (a <see cref="BoqLine"/> when <see cref="MeasurementSheet.CommercialDocumentType"/>
/// is Contract, a <see cref="SubcontractLine"/> when Subcontract). A child entity, not an independent
/// Business Object, same "0..n child collection, only exists through its parent" pattern as those two line
/// types. <see cref="QuantitySubmitted"/> and <see cref="QuantityCertified"/> are deliberately separate
/// fields — the Client's Engineer certifying a lower quantity than submitted is routine
/// (construction-commercial-processes-spec.md §2), not an edge case.
/// </summary>
public sealed class MeasurementLine
{
    public Guid Id { get; private set; }
    public Guid CommercialDocumentLineId { get; private set; }
    public decimal QuantitySubmitted { get; private set; }
    public decimal? QuantityCertified { get; private set; }
    public string? Remarks { get; private set; }

    internal MeasurementLine(Guid commercialDocumentLineId, decimal quantitySubmitted, string? remarks)
    {
        if (quantitySubmitted < 0)
            throw new ArgumentException("Quantity submitted cannot be negative.", nameof(quantitySubmitted));

        Id = Guid.NewGuid();
        CommercialDocumentLineId = commercialDocumentLineId;
        QuantitySubmitted = quantitySubmitted;
        Remarks = remarks;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private MeasurementLine()
    {
    }

    /// <summary>Set once, by <see cref="MeasurementSheet.RecordCertifiedQuantities"/> only — never called
    /// directly, so the "every line covered exactly once, only while Submitted" rule lives in exactly one
    /// place.</summary>
    internal void SetCertifiedQuantity(decimal quantityCertified)
    {
        if (quantityCertified < 0)
            throw new ArgumentException("Quantity certified cannot be negative.", nameof(quantityCertified));

        QuantityCertified = quantityCertified;
    }
}
