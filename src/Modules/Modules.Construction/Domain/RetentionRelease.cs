using Platform.Core;

namespace Modules.Construction.Domain;

/// <summary>
/// Which real-world event authorized releasing withheld retention back to the contractor
/// (construction-commercial-processes-spec.md §5). Structural, not a business policy (no company invents a
/// fourth kind of retention release), so a plain enum, same reasoning as <see cref="CommercialDocumentType"/>.
/// </summary>
public enum RetentionTriggerEvent
{
    TakingOver,
    DefectsLiabilityExpiry,
    Manual
}

/// <summary>
/// Releases a portion of the retention a customer (or main contractor) has been withholding from certified
/// <see cref="Ipc"/>s against a <see cref="Contract"/> or <see cref="Subcontract"/> — the money owed back to
/// the contractor once security is no longer needed (spec §5: retention "accumulates as a liability owed
/// back to the contractor," tracked as a running balance, not settled IPC-by-IPC). Polymorphic over
/// "commercial document" for the same reason <see cref="Ipc"/> and <see cref="MeasurementSheet"/> are — a
/// Subcontract's retention balance and release cycle is entirely independent of the main Contract's.
///
/// Deliberately a single header figure (<see cref="AmountReleased"/>), not a collection of per-IPC release
/// lines — the spec itself describes retention release as "one or two lump-sum events tied to project
/// milestones," not a line-by-line reconciliation, and <c>RetentionReleaseService.CreateAsync</c> already
/// validates <see cref="AmountReleased"/> against the real running balance (every Approved IPC's own
/// <c>RetentionAmount</c> for this same commercial document, minus every prior Approved release), so nothing
/// is lost by not tracking which specific IPC(s) the released amount notionally comes from.
///
/// Reuses the same Draft → Submitted → Approved/Rejected lifecycle as <see cref="Ipc"/>, and the same
/// certify-then-bill pattern: approving a Contract-type release raises a real AR Invoice (the customer now
/// owes the contractor this amount — <c>Modules.Finance.Contracts.ICustomerInvoicingService</c>), approving a
/// Subcontract-type release raises a real AP Invoice (the main contractor now owes the subcontractor —
/// <c>Modules.Finance.Contracts.IVendorInvoicingService</c>), both left in Draft for Finance to review,
/// exactly like <see cref="Ipc"/>'s own certification wiring.
/// </summary>
public sealed class RetentionRelease : BusinessObject
{
    public Guid ProjectId { get; private set; }
    public CommercialDocumentType CommercialDocumentType { get; private set; }
    public Guid CommercialDocumentId { get; private set; }
    public DateOnly ReleaseDate { get; private set; }
    public decimal AmountReleased { get; private set; }
    public RetentionTriggerEvent TriggerEvent { get; private set; }

    /// <summary>Which accounts a Contract-type release's eventual AR Invoice should post to — required at
    /// creation for a Contract release, always null for a Subcontract release. Mirrors <see cref="Ipc"/>'s
    /// own account fields exactly, including the "freeze financial facts at Draft time" reasoning.</summary>
    public Guid? RevenueAccountId { get; private set; }

    public Guid? ReceivableAccountId { get; private set; }

    /// <summary>Mirror of <see cref="RevenueAccountId"/>/<see cref="ReceivableAccountId"/> for a
    /// Subcontract-type release's eventual AP Invoice.</summary>
    public Guid? ExpenseAccountId { get; private set; }

    public Guid? PayableAccountId { get; private set; }

    public Guid? TaxCodeId { get; private set; }

    public Guid? VatAccountId { get; private set; }

    /// <summary>Set once, when a Contract-type release is approved and its AR Invoice is raised — see
    /// <c>RetentionReleaseService.ApproveInternalAsync</c>. Null for a Subcontract-type release.</summary>
    public Guid? LinkedArInvoiceId { get; private set; }

    /// <summary>Mirror of <see cref="LinkedArInvoiceId"/> for a Subcontract-type release's AP Invoice.</summary>
    public Guid? LinkedApInvoiceId { get; private set; }

    public RetentionRelease(
        string createdBy, Guid projectId, CommercialDocumentType commercialDocumentType, Guid commercialDocumentId,
        DateOnly releaseDate, decimal amountReleased, RetentionTriggerEvent triggerEvent,
        Guid? revenueAccountId = null, Guid? receivableAccountId = null, Guid? taxCodeId = null, Guid? vatAccountId = null,
        Guid? expenseAccountId = null, Guid? payableAccountId = null)
        : base(createdBy)
    {
        if (amountReleased <= 0)
            throw new ArgumentException("Amount released must be greater than zero.", nameof(amountReleased));

        ProjectId = projectId;
        CommercialDocumentType = commercialDocumentType;
        CommercialDocumentId = commercialDocumentId;
        ReleaseDate = releaseDate;
        AmountReleased = amountReleased;
        TriggerEvent = triggerEvent;
        RevenueAccountId = revenueAccountId;
        ReceivableAccountId = receivableAccountId;
        TaxCodeId = taxCodeId;
        VatAccountId = vatAccountId;
        ExpenseAccountId = expenseAccountId;
        PayableAccountId = payableAccountId;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s
    /// parameterless constructor. Never call from application code.</summary>
    private RetentionRelease()
    {
    }

    /// <summary>Records the AR Invoice raised for this release on approval — see
    /// <c>RetentionReleaseService.ApproveInternalAsync</c>. Only ever called once, for a Contract-type release.</summary>
    public void LinkArInvoice(Guid arInvoiceId) => LinkedArInvoiceId = arInvoiceId;

    /// <summary>Mirror of <see cref="LinkArInvoice"/> for a Subcontract-type release's AP Invoice.</summary>
    public void LinkApInvoice(Guid apInvoiceId) => LinkedApInvoiceId = apInvoiceId;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
