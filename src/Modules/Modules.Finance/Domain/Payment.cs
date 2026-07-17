using Platform.Core;

namespace Modules.Finance.Domain;

/// <summary>
/// One payment made to a vendor — closes `MISSING-FEATURES-AUDIT.md` Part 2 §16, the single biggest data-model
/// gap the audit found: before this class existed, nothing anywhere in this codebase could ever record
/// that an <see cref="APInvoice"/> was actually paid. Same Draft → Submit → Approve → Post → Reverse
/// lifecycle as <see cref="APInvoice"/>/<see cref="JournalEntry"/>; Posting a payment generates a real
/// linked <see cref="JournalEntry"/> (Dr each allocated invoice's own Payable account, Cr the paying
/// <see cref="BankAccount"/>'s linked G/L account) via <c>PaymentService.PostAsync</c>, the exact same
/// "the document and its posting are two separate, independently-audited, independently-reversible things"
/// pattern <c>APInvoiceService.PostAsync</c> already established.
///
/// Owns a child <see cref="PaymentAllocation"/> collection — a single payment can settle several invoices in
/// one bank transfer, and a single invoice can be paid across several installments, the same real-world
/// shape a SAP payment run's line items have. <see cref="Amount"/> is computed from the allocations, never
/// stored directly — the same "computed, never stored" precedent as <see cref="PurchaseOrder"/>'s Total
/// (frozen in effect once Posted, since allocations can only be added while Draft, mirroring
/// <see cref="JournalEntry.AddLine"/>'s own freeze-after-submit rule).
/// </summary>
public sealed class Payment : BusinessObject
{
    private readonly List<PaymentAllocation> _allocations = new();

    public Guid VendorId { get; private set; }

    public Guid BankAccountId { get; private set; }

    public DateOnly PaymentDate { get; private set; }

    /// <summary>An admin-configurable Lookup code (Lookup type <c>"PaymentMethod"</c>) — validated at the
    /// <c>PaymentService</c> layer against the Lookup engine, not a hardcoded enum, same pattern as
    /// <c>Modules.MasterData.Domain.BusinessRole.RoleType</c>.</summary>
    public string PaymentMethod { get; private set; }

    public string? Reference { get; private set; }

    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    public decimal Amount => _allocations.Sum(a => a.AllocatedAmount);

    /// <summary>Set once, when this payment is Posted — the G/L Journal Entry generated for it. Reversing
    /// this payment reverses that linked entry too (see <c>PaymentService.ReverseAsync</c>), and releases
    /// every allocated invoice's outstanding balance back up by the reversed amount.</summary>
    public Guid? LinkedJournalEntryId { get; private set; }

    public Payment(string createdBy, Guid vendorId, Guid bankAccountId, DateOnly paymentDate, string paymentMethod, string? reference)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentMethod);
        VendorId = vendorId;
        BankAccountId = bankAccountId;
        PaymentDate = paymentDate;
        PaymentMethod = paymentMethod;
        Reference = reference;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private Payment()
    {
        PaymentMethod = null!;
    }

    /// <summary>Allocates part (or all) of this payment against one AP Invoice. Only while Draft — same
    /// freeze-after-submit rule as <see cref="JournalEntry.AddLine"/>. One allocation per invoice per
    /// payment (a second installment against the same invoice is a second, separate Payment) — combine
    /// into a single line instead of allowing duplicates, so <see cref="Amount"/> unambiguously reflects
    /// "how much of invoice X does this one payment settle."</summary>
    public PaymentAllocation AddAllocation(Guid apInvoiceId, decimal allocatedAmount)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Allocations can only be added while the payment is in Draft.");
        if (allocatedAmount <= 0)
            throw new ArgumentException("Allocated amount must be positive.", nameof(allocatedAmount));
        if (_allocations.Any(a => a.APInvoiceId == apInvoiceId))
            throw new ArgumentException("This payment already has an allocation against that invoice.");

        var allocation = new PaymentAllocation(apInvoiceId, allocatedAmount);
        _allocations.Add(allocation);
        return allocation;
    }

    public void RemoveAllocation(Guid allocationId)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Allocations can only be removed while the payment is in Draft.");
        _allocations.RemoveAll(a => a.Id == allocationId);
    }

    public void LinkJournalEntry(Guid journalEntryId) => LinkedJournalEntryId = journalEntryId;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);

    /// <summary>Refuses to post a payment with no allocations — the same "structural guarantee checked
    /// here, not trusted to whoever approved it" reasoning as <see cref="JournalEntry.Post"/> refusing an
    /// unbalanced entry. Overpayment-against-any-single-invoice validation needs real outstanding-balance
    /// data from other Payments, which the Domain layer has no access to — that check lives in
    /// <c>PaymentService.PostAsync</c>, the same "Domain enforces structural rules, Application validates
    /// cross-aggregate references" split used throughout this platform.</summary>
    public void Post(string actor)
    {
        if (_allocations.Count == 0)
            throw new InvalidOperationException("A payment with no allocations cannot be posted.");

        Transition(BusinessObjectTransition.Post, actor);
    }

    public void Reverse(string actor) => Transition(BusinessObjectTransition.Reverse, actor);
}
