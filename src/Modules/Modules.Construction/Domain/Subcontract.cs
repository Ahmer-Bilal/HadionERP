using Platform.Core;

namespace Modules.Construction.Domain;

/// <summary>
/// A procurement document assigned to WBS elements, distinct from a standard
/// <c>Modules.Procurement.Domain.PurchaseOrder</c> because it carries real construction-industry commercial
/// terms a plain PO has no concept of — retention withheld per certified payment, mobilization advance, and
/// back-charges (ROADMAP.md's Phase 3 scope;
/// docs/architecture/07-integrated-project-controlling.md §4). References a
/// <c>Modules.ProjectManagement.Domain.Project</c> directly (validated Approved via <c>IProjectLookup</c>
/// at Create, exactly like <see cref="Contract"/>) rather than being nested under one — an optional
/// <see cref="ContractId"/> gives back-to-back traceability to the Customer Contract when this
/// subcontract's scope corresponds to it, the same "kept purely for traceability" role
/// <c>PurchaseOrder.RequestForQuotationId</c> plays. Stops at Approved like every commercial/organizational
/// BO so far — no Post/Reverse; a Subcontract is not itself a journal-posting document.
/// </summary>
public sealed class Subcontract : BusinessObject
{
    private readonly List<SubcontractLine> _lines = new();
    private readonly List<BackCharge> _backCharges = new();

    public Guid ProjectId { get; private set; }

    /// <summary>Optional back-to-back traceability to the Customer Contract this subcontract's scope
    /// corresponds to — not required, since a subcontract can exist for scope not itemized in any single
    /// main contract (e.g. a specialty subcontractor under a Lump Sum main contract).</summary>
    public Guid? ContractId { get; private set; }

    public Guid SubcontractorId { get; private set; }

    /// <summary>Percentage withheld per certified payment (commonly 5-10% in KSA construction contracts,
    /// docs/architecture/07-integrated-project-controlling.md §4). Carried as a commercial
    /// term only this slice — not yet wired to any actual withholding mechanics, since Payment has no
    /// retention-holdback concept yet (same "stored, not yet mechanically enforced" state as
    /// <see cref="Contract.AdvancePaymentPercentage"/>).</summary>
    public decimal? RetentionPercentage { get; private set; }

    public decimal? MobilizationAdvancePercentage { get; private set; }

    public int? DefectsLiabilityPeriodMonths { get; private set; }

    public IReadOnlyCollection<SubcontractLine> Lines => _lines.AsReadOnly();

    public IReadOnlyCollection<BackCharge> BackCharges => _backCharges.AsReadOnly();

    /// <summary>Computed, never entered by hand — mirrors <c>Contract.ContractValue</c>.</summary>
    public decimal SubcontractValue => _lines.Sum(l => l.Amount);

    public decimal TotalBackCharges => _backCharges.Sum(b => b.Amount);

    /// <summary>What the subcontractor is ultimately owed after back-charges — not itself posted anywhere
    /// yet (no Payment/IPC integration this slice), purely an informational figure on the document.</summary>
    public decimal NetPayableValue => SubcontractValue - TotalBackCharges;

    public Subcontract(
        string createdBy, Guid projectId, Guid? contractId, Guid subcontractorId,
        decimal? retentionPercentage, decimal? mobilizationAdvancePercentage, int? defectsLiabilityPeriodMonths)
        : base(createdBy)
    {
        if (retentionPercentage is < 0 or > 100)
            throw new ArgumentException("Retention percentage must be between 0 and 100.", nameof(retentionPercentage));
        if (mobilizationAdvancePercentage is < 0 or > 100)
            throw new ArgumentException("Mobilization advance percentage must be between 0 and 100.", nameof(mobilizationAdvancePercentage));
        if (defectsLiabilityPeriodMonths is < 0)
            throw new ArgumentException("Defects liability period cannot be negative.", nameof(defectsLiabilityPeriodMonths));

        ProjectId = projectId;
        ContractId = contractId;
        SubcontractorId = subcontractorId;
        RetentionPercentage = retentionPercentage;
        MobilizationAdvancePercentage = mobilizationAdvancePercentage;
        DefectsLiabilityPeriodMonths = defectsLiabilityPeriodMonths;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private Subcontract()
    {
    }

    /// <summary>Adds one scope-of-work line. Only while in Draft, same "frozen once submitted" rule as
    /// every other module's line collections. <paramref name="wbsElementId"/> must already belong to this
    /// Subcontract's Project — that cross-module check happens in <c>SubcontractService.CreateAsync</c>
    /// (this type has no dependency on ProjectManagement), not here.</summary>
    public SubcontractLine AddLine(
        string code, string description, string? descriptionArabic, string unitOfMeasure,
        decimal quantity, decimal rate, Guid wbsElementId)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the subcontract is in Draft.");
        if (_lines.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Line code '{code}' is already used in this subcontract.", nameof(code));

        var line = new SubcontractLine(code, description, descriptionArabic, unitOfMeasure, quantity, rate, wbsElementId);
        _lines.Add(line);
        return line;
    }

    /// <summary>Records a back-charge against the subcontractor. Only while Approved — a back-charge is a
    /// live-execution event (damage, rework, delay recovery), not a Draft-time line item, so it can only be
    /// recorded once the subcontract is actually in force.</summary>
    public BackCharge AddBackCharge(string description, decimal amount, DateOnly dateIncurred)
    {
        if (Status != BusinessObjectStatus.Approved)
            throw new InvalidOperationException("Back charges can only be recorded against an Approved subcontract.");

        var backCharge = new BackCharge(description, amount, dateIncurred);
        _backCharges.Add(backCharge);
        return backCharge;
    }

    /// <summary>Increases (or decreases) an existing line's quantity — the write-through an Approved
    /// <see cref="VariationOrder"/> performs. Mirrors <see cref="Contract.AdjustBoqLineQuantity"/>.</summary>
    public void AdjustLineQuantity(Guid lineId, decimal quantityDelta)
    {
        if (Status != BusinessObjectStatus.Approved)
            throw new InvalidOperationException("Line quantities can only be adjusted against an Approved subcontract.");
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new ArgumentException($"Line {lineId} does not belong to this subcontract.", nameof(lineId));
        line.AdjustQuantity(quantityDelta);
    }

    /// <summary>Adds a wholly new line via an Approved <see cref="VariationOrder"/>. Mirrors
    /// <see cref="Contract.AddBoqLineFromVariationOrder"/>.</summary>
    public SubcontractLine AddLineFromVariationOrder(
        string code, string description, string? descriptionArabic, string unitOfMeasure,
        decimal quantity, decimal rate, Guid wbsElementId)
    {
        if (Status != BusinessObjectStatus.Approved)
            throw new InvalidOperationException("New lines can only be added via a Variation Order against an Approved subcontract.");
        if (_lines.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Line code '{code}' is already used in this subcontract.", nameof(code));

        var line = new SubcontractLine(code, description, descriptionArabic, unitOfMeasure, quantity, rate, wbsElementId);
        _lines.Add(line);
        return line;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
