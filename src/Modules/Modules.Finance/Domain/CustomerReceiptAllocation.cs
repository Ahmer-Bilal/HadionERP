namespace Modules.Finance.Domain;

/// <summary>One line of a <see cref="CustomerReceipt"/> — how much of that receipt settles one specific
/// <see cref="ARInvoice"/>. Mirrors <see cref="PaymentAllocation"/> exactly, AR side.</summary>
public sealed class CustomerReceiptAllocation
{
    public Guid Id { get; private set; }

    public Guid ARInvoiceId { get; private set; }

    public decimal AllocatedAmount { get; private set; }

    internal CustomerReceiptAllocation(Guid arInvoiceId, decimal allocatedAmount)
    {
        Id = Guid.NewGuid();
        ARInvoiceId = arInvoiceId;
        AllocatedAmount = allocatedAmount;
    }

    private CustomerReceiptAllocation()
    {
    }
}
