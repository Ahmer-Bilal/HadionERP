namespace Platform.Core.Lifecycle;

/// <summary>
/// Thrown when a transition is attempted from a status that does not allow it.
/// This is the kernel refusing an illegal state change — it is not a business-rule failure
/// (those are guard delegates supplied by the caller), it is a structural one.
/// </summary>
public sealed class InvalidLifecycleTransitionException : Exception
{
    public InvalidLifecycleTransitionException(BusinessObjectStatus from, BusinessObjectTransition transition)
        : base($"Transition '{transition}' is not allowed from status '{from}'.")
    {
    }
}

/// <summary>
/// The single, shared state machine every Business Object's lifecycle is checked against.
/// See docs/architecture/02-business-object-model.md §1.1.
/// </summary>
public static class LifecycleEngine
{
    private static readonly Dictionary<(BusinessObjectStatus From, BusinessObjectTransition Transition), BusinessObjectStatus> Map = new()
    {
        [(BusinessObjectStatus.Draft, BusinessObjectTransition.Submit)] = BusinessObjectStatus.Submitted,

        [(BusinessObjectStatus.Submitted, BusinessObjectTransition.StartApproval)] = BusinessObjectStatus.InApproval,
        // Not every BO type has a configured workflow — a submitted BO with no workflow attached goes
        // straight to Approved (or Rejected) via the same commands a workflow engine would issue. These
        // two shortcuts are deliberately symmetric: a BO that can be auto-approved without a workflow
        // must equally be rejectable without one (e.g. a synchronous validation rule catching a problem
        // before any human approver is ever involved) — found missing during Modules.MasterData's
        // BusinessPartner build, where Reject only worked via InApproval and had no direct-from-Submitted
        // path, unlike Approve.
        [(BusinessObjectStatus.Submitted, BusinessObjectTransition.Approve)] = BusinessObjectStatus.Approved,
        [(BusinessObjectStatus.Submitted, BusinessObjectTransition.Reject)] = BusinessObjectStatus.Rejected,

        [(BusinessObjectStatus.InApproval, BusinessObjectTransition.Approve)] = BusinessObjectStatus.Approved,
        [(BusinessObjectStatus.InApproval, BusinessObjectTransition.Reject)] = BusinessObjectStatus.Rejected,

        [(BusinessObjectStatus.Rejected, BusinessObjectTransition.Resubmit)] = BusinessObjectStatus.Draft,

        [(BusinessObjectStatus.Approved, BusinessObjectTransition.Post)] = BusinessObjectStatus.Posted,

        [(BusinessObjectStatus.Posted, BusinessObjectTransition.Reverse)] = BusinessObjectStatus.Reversed,

        // Cancel is allowed from any pre-posted state — a document can be withdrawn any time before
        // it has a financial/quantity effect. Once Posted, the only correction path is Reverse.
        [(BusinessObjectStatus.Draft, BusinessObjectTransition.Cancel)] = BusinessObjectStatus.Cancelled,
        [(BusinessObjectStatus.Submitted, BusinessObjectTransition.Cancel)] = BusinessObjectStatus.Cancelled,
        [(BusinessObjectStatus.InApproval, BusinessObjectTransition.Cancel)] = BusinessObjectStatus.Cancelled,
        [(BusinessObjectStatus.Approved, BusinessObjectTransition.Cancel)] = BusinessObjectStatus.Cancelled,
        [(BusinessObjectStatus.Rejected, BusinessObjectTransition.Cancel)] = BusinessObjectStatus.Cancelled,
    };

    /// <summary>True if <paramref name="transition"/> is structurally legal from <paramref name="from"/>.</summary>
    public static bool CanApply(BusinessObjectStatus from, BusinessObjectTransition transition)
        => Map.ContainsKey((from, transition));

    /// <summary>
    /// Returns the resulting status, or throws <see cref="InvalidLifecycleTransitionException"/> if the
    /// transition is not legal from the current status. Callers apply their own business-rule guards
    /// (e.g. "PO requires second approval above 100,000 SAR") before calling this — this method only
    /// enforces the structural shape of the lifecycle, per doc 02 §1.1.
    /// </summary>
    public static BusinessObjectStatus Apply(BusinessObjectStatus from, BusinessObjectTransition transition)
    {
        if (!Map.TryGetValue((from, transition), out var to))
        {
            throw new InvalidLifecycleTransitionException(from, transition);
        }

        return to;
    }

    /// <summary>True once a Business Object has left Draft and may no longer be hard-deleted (doc 02 §1.1).</summary>
    public static bool IsPastDraft(BusinessObjectStatus status) => status != BusinessObjectStatus.Draft;
}
