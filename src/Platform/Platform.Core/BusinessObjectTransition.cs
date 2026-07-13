namespace Platform.Core;

/// <summary>
/// The guarded commands that move a Business Object between statuses.
/// Every module reuses these — a module never invents its own transition vocabulary.
/// </summary>
public enum BusinessObjectTransition
{
    Submit,
    StartApproval,
    Approve,
    Reject,
    Resubmit,
    Post,
    Cancel,
    Reverse
}
