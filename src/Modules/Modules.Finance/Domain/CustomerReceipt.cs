using Platform.Core;

namespace Modules.Finance.Domain;

/// <summary>
/// One payment received from a customer — the AR mirror of <see cref="Payment"/>, closing the gap that left
/// <see cref="ARInvoice.OutstandingBalance"/> permanently equal to Gross Amount. Same Draft → Submit →
/// Approve → Post → Reverse lifecycle; Posting generates a real linked <see cref="JournalEntry"/> (Dr the
/// receiving <see cref="BankAccount"/>'s linked G/L account, Cr each allocated invoice's own Receivable
/// account — the mirror image of Payment's Dr Payable/Cr Bank). Owns a child
/// <see cref="CustomerReceiptAllocation"/> collection for the same "one receipt can settle several invoices,
/// one invoice can be settled across several receipts" reason Payment does.
/// </summary>
public sealed class CustomerReceipt : BusinessObject
{
    private readonly List<CustomerReceiptAllocation> _allocations = new();

    public Guid CustomerId { get; private set; }

    public Guid BankAccountId { get; private set; }

    public DateOnly ReceiptDate { get; private set; }

    /// <summary>Same admin-configurable Lookup type ("PaymentMethod") <see cref="Payment"/> already uses —
    /// how the money arrived, not which direction it moved.</summary>
    public string PaymentMethod { get; private set; }

    public string? Reference { get; private set; }

    public IReadOnlyCollection<CustomerReceiptAllocation> Allocations => _allocations.AsReadOnly();

    public decimal Amount => _allocations.Sum(a => a.AllocatedAmount);

    public Guid? LinkedJournalEntryId { get; private set; }

    public CustomerReceipt(string createdBy, Guid customerId, Guid bankAccountId, DateOnly receiptDate, string paymentMethod, string? reference)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentMethod);
        CustomerId = customerId;
        BankAccountId = bankAccountId;
        ReceiptDate = receiptDate;
        PaymentMethod = paymentMethod;
        Reference = reference;
    }

    private CustomerReceipt()
    {
        PaymentMethod = null!;
    }

    /// <summary>Allocates part (or all) of this receipt against one AR Invoice. Only while Draft. One
    /// allocation per invoice per receipt, same reasoning as <see cref="Payment.AddAllocation"/>.</summary>
    public CustomerReceiptAllocation AddAllocation(Guid arInvoiceId, decimal allocatedAmount)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Allocations can only be added while the receipt is in Draft.");
        if (allocatedAmount <= 0)
            throw new ArgumentException("Allocated amount must be positive.", nameof(allocatedAmount));
        if (_allocations.Any(a => a.ARInvoiceId == arInvoiceId))
            throw new ArgumentException("This receipt already has an allocation against that invoice.");

        var allocation = new CustomerReceiptAllocation(arInvoiceId, allocatedAmount);
        _allocations.Add(allocation);
        return allocation;
    }

    public void RemoveAllocation(Guid allocationId)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Allocations can only be removed while the receipt is in Draft.");
        _allocations.RemoveAll(a => a.Id == allocationId);
    }

    public void LinkJournalEntry(Guid journalEntryId) => LinkedJournalEntryId = journalEntryId;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);

    public void Post(string actor)
    {
        if (_allocations.Count == 0)
            throw new InvalidOperationException("A receipt with no allocations cannot be posted.");

        Transition(BusinessObjectTransition.Post, actor);
    }

    public void Reverse(string actor) => Transition(BusinessObjectTransition.Reverse, actor);
}
