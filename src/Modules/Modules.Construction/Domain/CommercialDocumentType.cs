namespace Modules.Construction.Domain;

/// <summary>
/// Which commercial document a <see cref="MeasurementSheet"/> measures progress against — a
/// <see cref="Contract"/> or a <see cref="Subcontract"/>. Structural, not a business policy (no company
/// would want a third value), so a plain enum, not an admin-configurable Lookup — same reasoning as
/// <c>Platform.Core.BusinessObjectStatus</c>. Both document types live in this same module, so no
/// cross-module lookup is needed to resolve which repository to query.
/// </summary>
public enum CommercialDocumentType
{
    Contract,
    Subcontract
}
