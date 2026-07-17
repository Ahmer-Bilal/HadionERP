using Platform.Core;

namespace Modules.ProjectManagement.Domain;

/// <summary>
/// The Project Definition (docs/architecture/07-integrated-project-controlling.md §4) —
/// Phase 3's opening slice, the generic cost/schedule backbone every later project-based module
/// (Modules.Construction's Contracts/BOQ/Subcontracts, Procurement's future WBS-aware PO lines, Payroll's
/// future labor-cost postings) references instead of inventing its own "which project does this belong to"
/// concept. Stops at Approved ("released," SAP PS's own CRTD → REL status pair) like every Master-Data-ish
/// BO so far — a project must be released before other modules should be allowed to post cost against its
/// WBS elements (that enforcement is each future consumer's own job via a published lookup, not built here;
/// see the module README's Deferred section).
///
/// Owns a hierarchical <see cref="WbsElement"/> collection — the actual Controlling object doc 07 §4
/// describes ("a full Controlling object in its own right — cost, budget, revenue... can post directly
/// against it"). Networks/Activities/Milestones (scheduling, dependencies, resource/equipment allocation)
/// are doc 07 §4's other named piece of this module but are a separate, later slice — WBS is the
/// cost/Controlling backbone every downstream module needs first; scheduling has no other-module dependents
/// yet and is deferred, disclosed in the README rather than built speculatively.
/// </summary>
public sealed class Project : BusinessObject
{
    private readonly List<WbsElement> _wbsElements = new();

    public string ProjectName { get; private set; }

    public string? ProjectNameArabic { get; private set; }

    /// <summary>Optional — a project's sold-to party, validated (must exist, be Approved, hold the Client
    /// role) via <c>Modules.MasterData.Contracts.IBusinessPartnerLookup</c> when provided. Null for
    /// internal/overhead projects, which real construction groups also run through the same project
    /// structure.</summary>
    public Guid? CustomerId { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public IReadOnlyCollection<WbsElement> WbsElements => _wbsElements.AsReadOnly();

    public Project(
        string createdBy, string projectName, string? projectNameArabic, Guid? customerId,
        DateOnly? startDate, DateOnly? endDate)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ProjectName = projectName;
        ProjectNameArabic = projectNameArabic;
        CustomerId = customerId;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private Project()
    {
        ProjectName = null!;
    }

    /// <summary>Adds one WBS element to this project's hierarchy. Only while in Draft, same "frozen once
    /// submitted" rule as every other module's line collections — real WBS structure maintenance on an
    /// already-released project (adding scope mid-project) is deferred, disclosed in the README, not solved
    /// by this foundation slice. <paramref name="parentWbsElementId"/>, if given, must already belong to
    /// this project (build the hierarchy root-first).</summary>
    public WbsElement AddWbsElement(
        string code, string name, string? nameArabic, Guid? parentWbsElementId,
        bool isPlanningElement, bool isAccountAssignmentElement, bool isBillingElement)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("WBS elements can only be added while the project is in Draft.");
        if (_wbsElements.Any(w => w.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"WBS element code '{code}' is already used in this project.", nameof(code));
        if (parentWbsElementId is { } parentId && !_wbsElements.Any(w => w.Id == parentId))
            throw new ArgumentException($"Parent WBS element {parentId} does not belong to this project (or hasn't been added yet).", nameof(parentWbsElementId));

        var element = new WbsElement(code, name, nameArabic, parentWbsElementId, isPlanningElement, isAccountAssignmentElement, isBillingElement);
        _wbsElements.Add(element);
        return element;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
