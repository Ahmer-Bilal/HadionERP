namespace Modules.Finance.Domain;

/// <summary>One fixed row in the Period Closing Center's checklist — <see cref="Key"/>/<see cref="Title"/>/
/// <see cref="Description"/>/<see cref="SequenceNumber"/> read directly off `UI/Finance/d1f20165-...png`'s
/// own ten rows, not invented.</summary>
public sealed record ClosingActivityDefinition(string Key, int SequenceNumber, string Title, string Description);

/// <summary>
/// The ten fixed Closing Activities the mockup shows, in the mockup's own order — a closed catalog (not an
/// admin-configurable lookup type, unlike e.g. Country/UnitOfMeasure) because these specific ten rows and
/// their order come straight from the mockup's own design, the same "match the mockup exactly" instruction
/// that shaped every other field on this entity.
/// </summary>
public static class ClosingActivityCatalog
{
    public const string BankReconciliation = "BankReconciliation";
    public const string AccountsPayable = "AccountsPayable";
    public const string AccountsReceivable = "AccountsReceivable";
    public const string InventoryClosing = "InventoryClosing";
    public const string PayrollPosting = "PayrollPosting";
    public const string FixedAssets = "FixedAssets";
    public const string TaxValidation = "TaxValidation";
    public const string CostAllocation = "CostAllocation";
    public const string JournalReview = "JournalReview";
    public const string ManagementReview = "ManagementReview";

    public static readonly IReadOnlyList<ClosingActivityDefinition> All = new[]
    {
        new ClosingActivityDefinition(BankReconciliation, 1, "Bank Reconciliation", "Reconcile all bank accounts"),
        new ClosingActivityDefinition(AccountsPayable, 2, "Accounts Payable", "Verify and close AP transactions"),
        new ClosingActivityDefinition(AccountsReceivable, 3, "Accounts Receivable", "Verify and close AR transactions"),
        new ClosingActivityDefinition(InventoryClosing, 4, "Inventory Closing", "Value inventory and close period"),
        new ClosingActivityDefinition(PayrollPosting, 5, "Payroll Posting", "Post payroll and related liabilities"),
        new ClosingActivityDefinition(FixedAssets, 6, "Fixed Assets", "Depreciation and asset updates"),
        new ClosingActivityDefinition(TaxValidation, 7, "Tax Validation", "Validate VAT/GST & tax entries"),
        new ClosingActivityDefinition(CostAllocation, 8, "Cost Allocation", "Allocate costs to projects/depts."),
        new ClosingActivityDefinition(JournalReview, 9, "Journal Review", "Review manual journals"),
        new ClosingActivityDefinition(ManagementReview, 10, "Management Review", "Final review and sign-off"),
    };

    /// <summary>The three activities with a real underlying document type to derive steps from
    /// (Bank Accounts, AP/AR Invoices needing closure, Manual journals needing review) vs. the other six
    /// whose underlying module (Inventory/Payroll/Fixed Assets) or dedicated engine (Tax Validation, Cost
    /// Allocation, a final sign-off) doesn't exist yet in this platform — those get one manual, honestly
    /// generic step apiece rather than fabricated detail. See <see cref="ClosingActivityStep"/>'s own doc
    /// comment for how the auto-tracked three actually stay in sync with real document status.</summary>
    public static readonly IReadOnlySet<string> AutoTrackedKeys = new HashSet<string>
    {
        AccountsPayable, AccountsReceivable, JournalReview,
    };

    public static ClosingActivityDefinition Get(string key) =>
        All.FirstOrDefault(d => d.Key == key)
            ?? throw new ArgumentException($"'{key}' is not a known closing activity.", nameof(key));
}
