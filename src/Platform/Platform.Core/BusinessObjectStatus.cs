namespace Platform.Core;

/// <summary>
/// The one status vocabulary every Business Object in the platform uses.
/// See docs/architecture/02-business-object-model.md §1.1 — no module invents its own status names.
/// </summary>
public enum BusinessObjectStatus
{
    Draft,
    Submitted,
    InApproval,
    Approved,
    Rejected,
    Posted,
    Cancelled,
    Reversed
}
