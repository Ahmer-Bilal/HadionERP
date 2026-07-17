using Platform.Core;

namespace Modules.Construction.Domain;

/// <summary>
/// A scope/quantity change against a Contract or Subcontract — the mechanism
/// <c>MeasurementSheetService.CertifyAsync</c>'s over-measurement guard already points to ("An approved
/// Variation Order must increase the line's quantity first"). Polymorphic over "commercial document"
/// (<see cref="CommercialDocumentType"/> + <see cref="CommercialDocumentId"/>), same pattern as
/// <see cref="MeasurementSheet"/>/<c>Modules.Construction.Domain.Ipc</c> — a Subcontract needs its own
/// independent variation cycle against the main contractor, distinct from the Customer Contract's.
///
/// Stops at Draft → Submitted → Approved/Rejected, deliberately simple for this first slice (no Post/Reverse
/// — a Variation Order is not itself a journal-posting document, and unlike an IPC it never raises an
/// invoice by itself; its effect is a BOQ/scope change that later Measurement/IPC cycles bill against).
/// Approval is a two-step effect in <c>VariationOrderService.ApproveInternalAsync</c>: first the platform's
/// own workflow transition, then a write-through to the underlying Contract/Subcontract's own line collection
/// (<see cref="Contract.AdjustBoqLineQuantity"/>/<see cref="Contract.AddBoqLineFromVariationOrder"/> or their
/// Subcontract mirrors) — the one place in this module where an already-Approved commercial document's lines
/// change after the fact.
/// </summary>
public sealed class VariationOrder : BusinessObject
{
    private readonly List<VariationOrderLine> _lines = new();

    public Guid ProjectId { get; private set; }
    public CommercialDocumentType CommercialDocumentType { get; private set; }
    public Guid CommercialDocumentId { get; private set; }
    public string Reason { get; private set; }

    public IReadOnlyCollection<VariationOrderLine> Lines => _lines.AsReadOnly();

    /// <summary>Computed, never entered by hand — mirrors <see cref="Contract.ContractValue"/>. Net of both
    /// adjustment lines (which may be negative, an omission) and new lines.</summary>
    public decimal TotalValue => _lines.Sum(l => l.Amount);

    public VariationOrder(
        string createdBy, Guid projectId, CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, string reason)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ProjectId = projectId;
        CommercialDocumentType = commercialDocumentType;
        CommercialDocumentId = commercialDocumentId;
        Reason = reason;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private VariationOrder()
    {
        Reason = null!;
    }

    /// <summary>Adds a line adjusting an existing commercial document line's quantity by
    /// <paramref name="quantityDelta"/> (may be negative — an omission). Only while in Draft.
    /// <paramref name="commercialDocumentLineId"/> must belong to this order's commercial document and
    /// <paramref name="rate"/> is snapshotted from it — both cross-aggregate checks happen in
    /// <c>VariationOrderService.CreateAsync</c>, not here.</summary>
    public VariationOrderLine AddLineAdjustment(Guid commercialDocumentLineId, decimal quantityDelta, decimal rate)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the variation order is in Draft.");
        if (_lines.Any(l => l.CommercialDocumentLineId == commercialDocumentLineId))
            throw new ArgumentException(
                $"Line {commercialDocumentLineId} is already adjusted on this variation order.", nameof(commercialDocumentLineId));

        var line = new VariationOrderLine(commercialDocumentLineId, quantityDelta, rate);
        _lines.Add(line);
        return line;
    }

    /// <summary>Adds a line introducing a wholly new commercial document line. Only while in Draft.
    /// <paramref name="wbsElementId"/> must belong to this order's own Project — validated in
    /// <c>VariationOrderService.CreateAsync</c>, not here.</summary>
    public VariationOrderLine AddNewLine(
        string code, string description, string? descriptionArabic, string unitOfMeasure,
        decimal quantity, decimal rate, Guid wbsElementId)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the variation order is in Draft.");

        var line = new VariationOrderLine(code, description, descriptionArabic, unitOfMeasure, quantity, rate, wbsElementId);
        _lines.Add(line);
        return line;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
