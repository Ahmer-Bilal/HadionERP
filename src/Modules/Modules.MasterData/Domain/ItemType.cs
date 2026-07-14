namespace Modules.MasterData.Domain;

/// <summary>
/// How an Item is procured/consumed and tracked — the same three-way split SAP MM (Material Type) and
/// D365 (Product subtype) both make. Determines which future modules can reference it and how: Stock
/// items are warehouse-tracked (Inventory owns on-hand quantity, this module only owns the master record);
/// NonStock items (e.g. site consumables, one-off purchases) are expensed on receipt, never tracked as
/// inventory; Service items (e.g. subcontract labor, equipment rental hours) have no physical quantity at
/// all — a Procurement PO line or Construction BOQ line for a Service item is buying an activity, not a
/// countable thing.
/// </summary>
public enum ItemType
{
    Stock,
    NonStock,
    Service,
}
