using Platform.Core;

namespace Modules.MasterData.Domain;

/// <summary>
/// One material/product/service master record — the "Item" dimension a Procurement PO line, a
/// Construction BOQ line, or (later) an Inventory stock movement all reference, same role as SAP's
/// Material Master or D365's Released Product. This is the second of the two Phase 1 exit-criteria master-
/// data pieces alongside <see cref="GLAccount"/> and <see cref="BusinessPartner"/> ("maintain its chart of
/// accounts and vendors" plus the items/materials those vendors supply). Follows the standard Business
/// Object lifecycle (Draft → Submit → Approve) — adding a new item to the master is a real control point
/// (a miscoded or duplicate item pollutes every PO/BOQ line that references it afterward), same reasoning
/// as Business Partner and G/L Account onboarding.
///
/// Deliberately flat, no parent hierarchy: unlike <see cref="GLAccount"/>'s chart (which genuinely needs
/// multi-level roll-ups for financial statements), an Item catalog's grouping/categorization is a reporting
/// concern, not a structural one — deferred until a real need (e.g. Item Group master) shows up.
/// </summary>
public sealed class Item : BusinessObject
{
    /// <summary>The business-facing item code (e.g. "MAT-1010", "SVC-2001"), distinct from the sequential
    /// <see cref="BusinessObject.DocumentNumber"/> audit id. Must be unique — enforced by the service + a
    /// DB unique index.</summary>
    public string ItemCode { get; private set; }

    public string ItemName { get; private set; }

    /// <summary>The item's name in Arabic — same bilingual precedent as <see cref="BusinessPartner.NameArabic"/>
    /// and <see cref="GLAccount.AccountNameArabic"/>, for correctly localized Arabic purchase orders and
    /// BOQs.</summary>
    public string? ItemNameArabic { get; private set; }

    public ItemType ItemType { get; private set; }

    /// <summary>The unit an item's quantity is expressed in (e.g. "EA", "KG", "M3", "M2", "TON", "LM", "HR").
    /// Free text for Phase 1 — there is no UoM master with conversion factors yet (see Deferred in the
    /// module README); a real UoM master would replace this with a foreign key once a second unit or a
    /// conversion (e.g. bags → tons) is actually needed.</summary>
    public string UnitOfMeasure { get; private set; }

    /// <summary>True when the item accepts new transactions (POs, BOQ lines). Deactivating (rather than
    /// deleting) an item that already has history keeps prior documents valid while preventing new ones —
    /// the same "correct by reversal, not by deletion" principle used everywhere else in this platform.</summary>
    public bool IsActive { get; private set; }

    public Item(string createdBy, string itemCode, string itemName, ItemType itemType, string unitOfMeasure)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);
        ItemCode = itemCode;
        ItemName = itemName;
        ItemType = itemType;
        UnitOfMeasure = unitOfMeasure;
        IsActive = true;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private Item()
    {
        ItemCode = null!;
        ItemName = null!;
        UnitOfMeasure = null!;
    }

    public void UpdateItemName(string itemName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemName);
        ItemName = itemName;
    }

    public void UpdateItemNameArabic(string? itemNameArabic) => ItemNameArabic = itemNameArabic;

    public void UpdateUnitOfMeasure(string unitOfMeasure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);
        UnitOfMeasure = unitOfMeasure;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
