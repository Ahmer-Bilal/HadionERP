namespace Modules.Finance.Domain;

/// <summary>
/// One line of a <see cref="Payment"/> — how much of that payment settles one specific
/// <see cref="APInvoice"/>. A child entity, not an independent Business Object, same "0..n child
/// collection, only exists through its parent" pattern as <see cref="JournalLine"/> on
/// <see cref="JournalEntry"/>. Constructed only through <see cref="Payment.AddAllocation"/>.
/// </summary>
public sealed class PaymentAllocation
{
    public Guid Id { get; private set; }

    public Guid APInvoiceId { get; private set; }

    public decimal AllocatedAmount { get; private set; }

    internal PaymentAllocation(Guid apInvoiceId, decimal allocatedAmount)
    {
        Id = Guid.NewGuid();
        APInvoiceId = apInvoiceId;
        AllocatedAmount = allocatedAmount;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private PaymentAllocation()
    {
    }
}
