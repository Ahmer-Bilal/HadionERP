using Platform.Core;

namespace Modules.Construction.Domain;

/// <summary>
/// The commercial contract a <c>Modules.ProjectManagement.Domain.Project</c> is executed against
/// (docs/architecture/07-integrated-project-controlling.md §4's Construction layer —
/// "Customer Contracts... referencing WBS elements"). Owns a <see cref="BoqLine"/> collection — the
/// line-by-line quantity/rate breakdown mapped onto the Project's WBS elements, priced by this Contract.
/// Stops at Approved like every commercial/organizational BO so far (no Post/Reverse — a Contract is not
/// itself a journal-posting document; Interim Payment Certificate billing against it, a later slice, is
/// what actually posts to Finance). Not enforced as one-per-project: real contracts get amendments, and
/// forcing a 1:1 relationship here would need reworking the moment a genuine amendment/addendum contract
/// shows up — left open deliberately, disclosed in the module README.
/// </summary>
public sealed class Contract : BusinessObject
{
    private readonly List<BoqLine> _boqLines = new();

    public Guid ProjectId { get; private set; }

    /// <summary>Lookup-validated against the <c>ContractType</c> Lookup type (LumpSum/UnitPrice/CostPlus) —
    /// same <c>ILookupCatalog</c> pattern <c>Modules.Finance.Application.PaymentService</c> already uses for
    /// <c>PaymentMethod</c>.</summary>
    public string ContractType { get; private set; }

    /// <summary>Free text for this slice — the real Payment-Terms field belongs on the Business Partner
    /// master (<c>MISSING-FEATURES-AUDIT.md</c> §15, still open), not duplicated here as a separate concept.</summary>
    public string? PaymentTerms { get; private set; }

    public decimal? AdvancePaymentPercentage { get; private set; }

    public int? DefectsLiabilityPeriodMonths { get; private set; }

    public IReadOnlyCollection<BoqLine> BoqLines => _boqLines.AsReadOnly();

    /// <summary>Computed, never entered by hand — mirrors <c>Modules.Procurement.Domain.PurchaseOrder.Total</c>.</summary>
    public decimal ContractValue => _boqLines.Sum(l => l.Amount);

    public Contract(
        string createdBy, Guid projectId, string contractType, string? paymentTerms,
        decimal? advancePaymentPercentage, int? defectsLiabilityPeriodMonths)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contractType);
        if (advancePaymentPercentage is < 0 or > 100)
            throw new ArgumentException("Advance payment percentage must be between 0 and 100.", nameof(advancePaymentPercentage));
        if (defectsLiabilityPeriodMonths is < 0)
            throw new ArgumentException("Defects liability period cannot be negative.", nameof(defectsLiabilityPeriodMonths));

        ProjectId = projectId;
        ContractType = contractType;
        PaymentTerms = paymentTerms;
        AdvancePaymentPercentage = advancePaymentPercentage;
        DefectsLiabilityPeriodMonths = defectsLiabilityPeriodMonths;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private Contract()
    {
        ContractType = null!;
    }

    /// <summary>Adds one BOQ line. Only while in Draft, same "frozen once submitted" rule as every other
    /// module's line collections. <paramref name="wbsElementId"/> must already belong to this Contract's
    /// Project — that cross-module check happens in <c>ContractService.CreateAsync</c> (this type has no
    /// dependency on ProjectManagement), not here.</summary>
    public BoqLine AddBoqLine(
        string code, string description, string? descriptionArabic, string unitOfMeasure,
        decimal quantity, decimal rate, Guid wbsElementId)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("BOQ lines can only be added while the contract is in Draft.");
        if (_boqLines.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"BOQ line code '{code}' is already used in this contract.", nameof(code));

        var line = new BoqLine(code, description, descriptionArabic, unitOfMeasure, quantity, rate, wbsElementId);
        _boqLines.Add(line);
        return line;
    }

    /// <summary>Increases (or decreases) an existing BOQ line's quantity — the write-through an Approved
    /// <see cref="VariationOrder"/> performs, called from <c>VariationOrderService.ApproveInternalAsync</c>.
    /// Only while the Contract itself is Approved, since this is a live-execution change, not a Draft-time
    /// edit like <see cref="AddBoqLine"/>.</summary>
    public void AdjustBoqLineQuantity(Guid lineId, decimal quantityDelta)
    {
        if (Status != BusinessObjectStatus.Approved)
            throw new InvalidOperationException("BOQ line quantities can only be adjusted against an Approved contract.");
        var line = _boqLines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new ArgumentException($"BOQ line {lineId} does not belong to this contract.", nameof(lineId));
        line.AdjustQuantity(quantityDelta);
    }

    /// <summary>Adds a wholly new BOQ line via an Approved <see cref="VariationOrder"/> — the other
    /// write-through it performs. Mirrors <see cref="AddBoqLine"/>'s own validation, but only while the
    /// Contract itself is Approved, not Draft.</summary>
    public BoqLine AddBoqLineFromVariationOrder(
        string code, string description, string? descriptionArabic, string unitOfMeasure,
        decimal quantity, decimal rate, Guid wbsElementId)
    {
        if (Status != BusinessObjectStatus.Approved)
            throw new InvalidOperationException("New BOQ lines can only be added via a Variation Order against an Approved contract.");
        if (_boqLines.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"BOQ line code '{code}' is already used in this contract.", nameof(code));

        var line = new BoqLine(code, description, descriptionArabic, unitOfMeasure, quantity, rate, wbsElementId);
        _boqLines.Add(line);
        return line;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
