namespace Modules.Construction.Domain;

/// <summary>
/// One line of an <see cref="Ipc"/> — mirrors the source <see cref="MeasurementLine"/> it was generated
/// from, but carries the money math the measurement itself deliberately doesn't (construction-commercial-
/// processes-spec.md §3). <see cref="Rate"/> is a snapshot from the commercial document's own line at IPC
/// creation time, not a live reference — an IPC is a point-in-time billing calculation and must stay
/// arithmetically stable even if the underlying BOQ/Subcontract line could theoretically change later.
/// <see cref="QuantityToDate"/> is the cumulative certified quantity across every Approved
/// <see cref="MeasurementSheet"/> ever measured against this same commercial document line (computed once,
/// by <c>IpcService.CreateAsync</c>, via the same cross-sheet aggregation
/// <c>MeasurementSheetService</c>'s over-measurement guard already uses) — <see cref="QuantityThisPeriod"/>
/// is just the source sheet's own certified quantity, since the over-measurement guard already guarantees
/// no double-counting across sheets.
/// </summary>
public sealed class IpcLine
{
    public Guid Id { get; private set; }
    public Guid CommercialDocumentLineId { get; private set; }
    public decimal Rate { get; private set; }
    public decimal QuantityThisPeriod { get; private set; }
    public decimal QuantityToDate { get; private set; }

    public decimal ValueThisPeriod => QuantityThisPeriod * Rate;
    public decimal ValueToDate => QuantityToDate * Rate;

    internal IpcLine(Guid commercialDocumentLineId, decimal rate, decimal quantityThisPeriod, decimal quantityToDate)
    {
        if (rate <= 0) throw new ArgumentException("Rate must be greater than zero.", nameof(rate));
        if (quantityThisPeriod < 0) throw new ArgumentException("Quantity this period cannot be negative.", nameof(quantityThisPeriod));
        if (quantityToDate < quantityThisPeriod)
            throw new ArgumentException("Quantity to date cannot be less than the quantity certified this period.", nameof(quantityToDate));

        Id = Guid.NewGuid();
        CommercialDocumentLineId = commercialDocumentLineId;
        Rate = rate;
        QuantityThisPeriod = quantityThisPeriod;
        QuantityToDate = quantityToDate;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private IpcLine()
    {
    }
}
