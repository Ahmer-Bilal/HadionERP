using Platform.Core;

namespace Modules.Construction.Domain;

/// <summary>
/// Periodic record of physical work actually done against a Contract or Subcontract's lines — the evidence
/// an IPC (a later slice, construction-commercial-processes-spec.md §3) bills against; you cannot bill for
/// work that hasn't been measured and certified. Polymorphic over "commercial document"
/// (<see cref="CommercialDocumentType"/> + <see cref="CommercialDocumentId"/>) from this first slice, per
/// the spec's §6c/§7/§8 and ROADMAP.md's "Construction commercial-process sequencing" decision — a
/// Subcontract needs its own independent measurement/IPC cycle against the main contractor, and
/// retrofitting this after a Contract-only build would mean reworking every measurement/IPC table.
///
/// Reuses the platform's own Draft → Submitted → Approved/Rejected lifecycle unchanged for the spec's
/// two-party certification workflow (site QS submits, Client's Engineer certifies) — "Approved" here IS
/// this domain's "Certified," not a new engine state (docs/architecture/02-business-object-model.md §1.1:
/// no module invents its own status names). Stops at Approved like every other commercial/organizational BO
/// in this module so far — Measurement itself never posts to Finance; IPC does that.
///
/// This slice finally makes the <c>IsBillingElement</c> WBS flag load-bearing (validated in
/// <c>MeasurementSheetService.CreateAsync</c>, this type has no dependency on ProjectManagement) — every
/// prior Construction BO accepted any WBS element, unrestricted, exactly as the spec's §2 anticipated.
/// </summary>
public sealed class MeasurementSheet : BusinessObject
{
    private readonly List<MeasurementLine> _lines = new();

    public Guid ProjectId { get; private set; }
    public CommercialDocumentType CommercialDocumentType { get; private set; }
    public Guid CommercialDocumentId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<MeasurementLine> Lines => _lines.AsReadOnly();

    public MeasurementSheet(
        string createdBy, Guid projectId, CommercialDocumentType commercialDocumentType, Guid commercialDocumentId,
        DateOnly periodStart, DateOnly periodEnd, string? notes)
        : base(createdBy)
    {
        if (periodEnd < periodStart)
            throw new ArgumentException("Period end cannot be before period start.", nameof(periodEnd));

        ProjectId = projectId;
        CommercialDocumentType = commercialDocumentType;
        CommercialDocumentId = commercialDocumentId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Notes = notes;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private MeasurementSheet()
    {
    }

    /// <summary>Adds one measurement line against a line of this sheet's own commercial document. Only
    /// while in Draft, same "frozen once submitted" rule as every other module's line collections.
    /// <paramref name="commercialDocumentLineId"/> must belong to this sheet's commercial document and
    /// reference an <c>IsBillingElement</c>-flagged WBS element — both cross-module checks happen in
    /// <c>MeasurementSheetService.CreateAsync</c>, not here.</summary>
    public MeasurementLine AddLine(Guid commercialDocumentLineId, decimal quantitySubmitted, string? remarks)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the measurement sheet is in Draft.");
        if (_lines.Any(l => l.CommercialDocumentLineId == commercialDocumentLineId))
            throw new ArgumentException(
                $"Line {commercialDocumentLineId} is already measured on this sheet.", nameof(commercialDocumentLineId));

        var line = new MeasurementLine(commercialDocumentLineId, quantitySubmitted, remarks);
        _lines.Add(line);
        return line;
    }

    /// <summary>Records the Engineer's certified quantity for every line on the sheet — may differ from
    /// <see cref="MeasurementLine.QuantitySubmitted"/> per line (a lower certified quantity than submitted
    /// is routine, spec §2), so every line must be supplied explicitly rather than defaulted. The
    /// over-measurement guard (cumulative certified-to-date must not exceed the referenced line's own
    /// Quantity) is a cross-aggregate check needing sibling-sheet history this type has no access to — it
    /// runs in <c>MeasurementSheetService.CertifyAsync</c> before this is ever called. Only while Submitted
    /// — this is the Engineer's review action, not a Draft-time edit.</summary>
    public void RecordCertifiedQuantities(IReadOnlyDictionary<Guid, decimal> certifiedQuantitiesByLineId)
    {
        if (Status != BusinessObjectStatus.Submitted)
            throw new InvalidOperationException("Certified quantities can only be recorded while the sheet is Submitted.");
        if (certifiedQuantitiesByLineId.Count != _lines.Count || _lines.Any(l => !certifiedQuantitiesByLineId.ContainsKey(l.Id)))
            throw new ArgumentException("Certified quantities must be supplied for every line on the sheet, exactly once.");

        foreach (var line in _lines)
            line.SetCertifiedQuantity(certifiedQuantitiesByLineId[line.Id]);
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
