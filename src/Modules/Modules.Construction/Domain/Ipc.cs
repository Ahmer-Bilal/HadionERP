using Platform.Core;

namespace Modules.Construction.Domain;

/// <summary>
/// The Interim Payment Certificate — the periodic billing document a contractor issues against certified
/// measurement (construction-commercial-processes-spec.md §3), generated from exactly one Approved
/// <see cref="MeasurementSheet"/> (<see cref="MeasurementSheetId"/>, enforced one-IPC-per-sheet by
/// <c>IpcService.CreateAsync</c> so the same period can never be billed twice). Polymorphic over
/// "commercial document" (<see cref="CommercialDocumentType"/> + <see cref="CommercialDocumentId"/>) for
/// the same reason <see cref="MeasurementSheet"/> is — a Subcontract runs its own independent IPC cycle
/// against the main contractor (spec §6c), not a child of the main Contract's IPC.
///
/// Reuses the platform's Draft → Submitted → Approved/Rejected lifecycle unchanged for the spec's Draft
/// (contractor prepares) → Submitted (to Engineer) → Certified (Engineer issues the Payment Certificate)
/// workflow — "Approved" here IS "Certified," same convention <see cref="MeasurementSheet"/> already
/// established. Deliberately stops at Approved, not the spec's further "Paid" step — closing that loop
/// means raising a real AR/Customer Invoice or WIP entry in Finance, and PROGRESS.md's 2026-07-16 entry
/// explicitly left "does IPC certification raise an AR invoice immediately, or a WIP step first" as an open
/// decision that depends on Finance's still-nonexistent AR module — not guessed here, disclosed in
/// `docs/module/construction.md` as the next real gap this slice exposes rather than closes.
///
/// The money waterfall is deliberately simple relative to the spec's own step numbering: Retention and
/// Advance Recovery are both calculated as a straight percentage of THIS PERIOD's certified value (not the
/// cumulative-to-date value the spec's literal wording could be read as), which is standard real-world IPC
/// practice — each certified amount has its own pro-rata retention/advance-recovery deducted, not one lump
/// deduction recalculated off a growing cumulative base every period.
/// </summary>
public sealed class Ipc : BusinessObject
{
    private readonly List<IpcLine> _lines = new();

    public Guid ProjectId { get; private set; }
    public CommercialDocumentType CommercialDocumentType { get; private set; }
    public Guid CommercialDocumentId { get; private set; }
    public Guid MeasurementSheetId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }

    /// <summary>Snapshotted from the commercial document at creation time — see <see cref="IpcLine.Rate"/>
    /// for why an IPC never reads these live from the Contract/Subcontract after the fact.</summary>
    public decimal? RetentionPercentageApplied { get; private set; }

    public decimal? AdvancePaymentPercentageApplied { get; private set; }

    /// <summary>Liquidated damages, material-on-site advance recovery, back-charges, and prior over-
    /// certification corrections all belong here per the spec's waterfall step 6 — none of those mechanisms
    /// exist yet, so this is genuinely manual entry for this slice, not yet computed from anything.</summary>
    public decimal OtherDeductions { get; private set; }

    public IReadOnlyCollection<IpcLine> Lines => _lines.AsReadOnly();

    public decimal GrossValueToDate => _lines.Sum(l => l.ValueToDate);
    public decimal GrossValueThisPeriod => _lines.Sum(l => l.ValueThisPeriod);
    public decimal GrossValuePreviousIpc => GrossValueToDate - GrossValueThisPeriod;
    public decimal RetentionAmount => GrossValueThisPeriod * (RetentionPercentageApplied ?? 0m) / 100m;
    public decimal AdvanceRecoveryAmount => GrossValueThisPeriod * (AdvancePaymentPercentageApplied ?? 0m) / 100m;
    public decimal NetPayable => GrossValueThisPeriod - RetentionAmount - AdvanceRecoveryAmount - OtherDeductions;

    public Ipc(
        string createdBy, Guid projectId, CommercialDocumentType commercialDocumentType, Guid commercialDocumentId,
        Guid measurementSheetId, DateOnly periodStart, DateOnly periodEnd,
        decimal? retentionPercentageApplied, decimal? advancePaymentPercentageApplied, decimal otherDeductions)
        : base(createdBy)
    {
        if (periodEnd < periodStart)
            throw new ArgumentException("Period end cannot be before period start.", nameof(periodEnd));
        if (retentionPercentageApplied is < 0 or > 100)
            throw new ArgumentException("Retention percentage must be between 0 and 100.", nameof(retentionPercentageApplied));
        if (advancePaymentPercentageApplied is < 0 or > 100)
            throw new ArgumentException("Advance payment percentage must be between 0 and 100.", nameof(advancePaymentPercentageApplied));
        if (otherDeductions < 0)
            throw new ArgumentException("Other deductions cannot be negative.", nameof(otherDeductions));

        ProjectId = projectId;
        CommercialDocumentType = commercialDocumentType;
        CommercialDocumentId = commercialDocumentId;
        MeasurementSheetId = measurementSheetId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        RetentionPercentageApplied = retentionPercentageApplied;
        AdvancePaymentPercentageApplied = advancePaymentPercentageApplied;
        OtherDeductions = otherDeductions;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private Ipc()
    {
    }

    /// <summary>Adds one billing line. Only while in Draft, same "frozen once submitted" rule as every other
    /// module's line collections. All cross-aggregate resolution (the commercial document's Rate for this
    /// line, the cumulative QuantityToDate) happens in <c>IpcService.CreateAsync</c>, not here.</summary>
    public IpcLine AddLine(Guid commercialDocumentLineId, decimal rate, decimal quantityThisPeriod, decimal quantityToDate)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the IPC is in Draft.");
        if (_lines.Any(l => l.CommercialDocumentLineId == commercialDocumentLineId))
            throw new ArgumentException(
                $"Line {commercialDocumentLineId} is already billed on this IPC.", nameof(commercialDocumentLineId));

        var line = new IpcLine(commercialDocumentLineId, rate, quantityThisPeriod, quantityToDate);
        _lines.Add(line);
        return line;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
