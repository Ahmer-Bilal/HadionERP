namespace Modules.Finance.Application;

public sealed record CustomerReceiptAllocationDto(Guid Id, Guid ARInvoiceId, decimal AllocatedAmount);

public sealed record CustomerReceiptDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid CustomerId,
    Guid BankAccountId,
    DateOnly ReceiptDate,
    string PaymentMethod,
    string? Reference,
    IReadOnlyList<CustomerReceiptAllocationDto> Allocations,
    decimal Amount,
    Guid? LinkedJournalEntryId,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateCustomerReceiptAllocationRequest(Guid ARInvoiceId, decimal AllocatedAmount);

public sealed record CreateCustomerReceiptRequest(
    Guid CustomerId,
    Guid BankAccountId,
    DateOnly ReceiptDate,
    string PaymentMethod,
    IReadOnlyList<CreateCustomerReceiptAllocationRequest> Allocations,
    string? Reference = null);
