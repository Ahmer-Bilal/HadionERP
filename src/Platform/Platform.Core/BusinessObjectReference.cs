namespace Platform.Core;

/// <summary>
/// A typed link from one Business Object to another (e.g. a GRN's reference back to the PO it was
/// received against). This is what drives the "Related documents" section of the record form
/// (docs/architecture/02-business-object-model.md §2.1) and cross-module navigation (doc 02 §3).
/// </summary>
public sealed record BusinessObjectReference(Guid TargetId, string TargetType, string RelationKind);
