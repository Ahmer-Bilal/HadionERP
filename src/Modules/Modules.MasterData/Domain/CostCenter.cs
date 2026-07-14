using Platform.Core;

namespace Modules.MasterData.Domain;

/// <summary>
/// One Cost Center — the "who owns this cost/revenue organizationally" Controlling object every journal
/// line can carry alongside the G/L Account
/// (docs/architecture/07-project-accounting-and-financial-architecture.md #1, #4), and the third of the
/// Phase 1 Master Data pieces. Follows the standard Business Object lifecycle (Draft → Submit → Approve),
/// same control-point reasoning as Business Partner/G/L Account/Item: a miscoded or duplicate cost center
/// pollutes every posting that references it afterward.
///
/// Mirrors <see cref="GLAccount"/>'s shape, not <see cref="Item"/>'s: a self-referencing parent hierarchy
/// (e.g. "Head Office" → "Finance Department" → "Payables Section") because cost center reporting
/// genuinely needs roll-ups, the same reason the chart of accounts needs one — unlike Item's flat catalog,
/// which has no such structural need at Phase 1. <see cref="IsPostable"/> mirrors
/// <see cref="GLAccount.IsPostable"/> for the same reason: a grouping/header cost center used only to
/// structure the hierarchy should never receive a direct posting.
/// </summary>
public sealed class CostCenter : BusinessObject
{
    /// <summary>The business-facing cost center code (e.g. "CC-1000", "CC-2010-HR"), distinct from the
    /// sequential <see cref="BusinessObject.DocumentNumber"/> audit id. Must be unique — enforced by the
    /// service + a DB unique index.</summary>
    public string CostCenterCode { get; private set; }

    public string CostCenterName { get; private set; }

    /// <summary>The cost center's name in Arabic — same bilingual precedent as
    /// <see cref="BusinessPartner.NameArabic"/>, <see cref="GLAccount.AccountNameArabic"/>, and
    /// <see cref="Item.ItemNameArabic"/>.</summary>
    public string? CostCenterNameArabic { get; private set; }

    /// <summary>Optional parent cost center for the reporting hierarchy (e.g. "Finance Department" →
    /// "Head Office"). Null for a top-level cost center. Self-referencing.</summary>
    public Guid? ParentCostCenterId { get; private set; }

    /// <summary>True for a leaf cost center journal lines can post to; false for a header/grouping cost
    /// center used only to structure the hierarchy. Prevents posting to a roll-up node.</summary>
    public bool IsPostable { get; private set; }

    /// <summary>True when the cost center accepts new postings. Deactivating (rather than deleting) a cost
    /// center that already has history keeps prior postings valid while preventing new ones — the same
    /// "correct by reversal, not by deletion" principle used everywhere else in this platform.</summary>
    public bool IsActive { get; private set; }

    public CostCenter(string createdBy, string costCenterCode, string costCenterName)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(costCenterCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(costCenterName);
        CostCenterCode = costCenterCode;
        CostCenterName = costCenterName;
        IsPostable = true;
        IsActive = true;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private CostCenter()
    {
        CostCenterCode = null!;
        CostCenterName = null!;
    }

    public void UpdateCostCenterName(string costCenterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(costCenterName);
        CostCenterName = costCenterName;
    }

    public void UpdateCostCenterNameArabic(string? costCenterNameArabic) => CostCenterNameArabic = costCenterNameArabic;

    public void AssignParent(Guid? parentCostCenterId) => ParentCostCenterId = parentCostCenterId;

    public void SetPostable(bool isPostable) => IsPostable = isPostable;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
