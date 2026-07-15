namespace Modules.ProjectManagement.Domain;

/// <summary>
/// One node in a <see cref="Project"/>'s Work Breakdown Structure — a child entity, not an independent
/// Business Object (same "0..n child collection, only exists through its parent" pattern as every other
/// module's line collections), but a real <b>Controlling object</b> per
/// docs/architecture/07-project-accounting-and-financial-architecture.md §4: once this project is Approved,
/// this element's <see cref="Id"/> is the thing other modules will eventually post cost/revenue/budget
/// against (a future Procurement PO line, a future Payroll labor-cost line) — the same role a Cost Center
/// plays for non-project costs.
///
/// The three flags mirror SAP PS's own WBS element flags exactly (doc 07 §4's table): a header/rollup node
/// is typically none of the three, a node actual costs are recorded against sets
/// <see cref="IsAccountAssignmentElement"/>, a node budgets are planned against sets
/// <see cref="IsPlanningElement"/>, and a node revenue/billing is recognized against (usually a higher-level
/// node) sets <see cref="IsBillingElement"/> — a single element can hold more than one flag.
/// </summary>
public sealed class WbsElement
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? NameArabic { get; private set; }
    public Guid? ParentWbsElementId { get; private set; }
    public bool IsPlanningElement { get; private set; }
    public bool IsAccountAssignmentElement { get; private set; }
    public bool IsBillingElement { get; private set; }

    internal WbsElement(
        string code, string name, string? nameArabic, Guid? parentWbsElementId,
        bool isPlanningElement, bool isAccountAssignmentElement, bool isBillingElement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        NameArabic = nameArabic;
        ParentWbsElementId = parentWbsElementId;
        IsPlanningElement = isPlanningElement;
        IsAccountAssignmentElement = isAccountAssignmentElement;
        IsBillingElement = isBillingElement;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private WbsElement()
    {
        Code = null!;
        Name = null!;
    }
}
