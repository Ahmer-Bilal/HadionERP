namespace Modules.Finance.Domain;

/// <summary>
/// One line within a <see cref="ClosingActivity"/>'s own checklist — the mockup's per-row "x/y" fraction
/// (e.g. Bank Reconciliation "12/12") made real rather than a decorative count. Two different kinds, per
/// <see cref="ClosingActivityCatalog.AutoTrackedKeys"/>:
///
/// <b>Auto-tracked</b> (Accounts Payable/Receivable, Journal Review) — one step per real document
/// (<see cref="LinkedDocumentType"/>/<see cref="LinkedDocumentId"/>, the exact same source-document-trace
/// shape <see cref="JournalEntry.SourceDocumentType"/> already established) that needed closing/review when
/// the checklist was generated. <see cref="IsCompleted"/> is never toggled by hand for these — it's
/// recomputed by <c>ClosingActivityService</c> against that document's *live* status each time the
/// checklist is fetched, so it always reflects reality rather than a snapshot that could drift.
///
/// <b>Manual</b> (the other seven activities) — no real per-item data exists yet (Bank Reconciliation has no
/// automated matching engine; Inventory/Payroll/Fixed Assets modules don't exist yet; Tax Validation/Cost
/// Allocation/Management Review have no dedicated engine) so each gets one honestly generic step, toggled by
/// hand by the assigned person — a real action, just not an automated one.
/// </summary>
public sealed class ClosingActivityStep
{
    public Guid Id { get; private set; }

    public string Description { get; private set; }

    /// <summary>Null for a manual step. Set for an auto-tracked one — which real document this step
    /// tracks, e.g. <c>"APInvoice"</c> + that invoice's own Id.</summary>
    public string? LinkedDocumentType { get; private set; }

    public Guid? LinkedDocumentId { get; private set; }

    public bool IsCompleted { get; private set; }

    public string? CompletedBy { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    internal ClosingActivityStep(string description, string? linkedDocumentType, Guid? linkedDocumentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Id = Guid.NewGuid();
        Description = description;
        LinkedDocumentType = linkedDocumentType;
        LinkedDocumentId = linkedDocumentId;
        IsCompleted = false;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private ClosingActivityStep()
    {
        Description = null!;
    }

    public bool IsAutoTracked => LinkedDocumentId is not null;

    /// <summary>Manual toggle — <c>ClosingActivityService</c> rejects calling this on an auto-tracked step
    /// (see this class's own doc comment); <see cref="SetCompletionFromLiveStatus"/> is that step kind's
    /// only path to a status change.</summary>
    public void SetCompleted(bool isCompleted, string actor)
    {
        IsCompleted = isCompleted;
        CompletedBy = isCompleted ? actor : null;
        CompletedAt = isCompleted ? DateTimeOffset.UtcNow : null;
    }

    /// <summary>Called only for an auto-tracked step, with the live status of the document it tracks — the
    /// mechanism that keeps this step honest without a background job or a human re-checking a box.</summary>
    public void SetCompletionFromLiveStatus(bool isCompleted)
    {
        if (IsCompleted == isCompleted) return;
        IsCompleted = isCompleted;
        CompletedBy = isCompleted ? "system/auto-tracked" : null;
        CompletedAt = isCompleted ? DateTimeOffset.UtcNow : null;
    }
}
