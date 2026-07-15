namespace Modules.Finance.Application;

public sealed record PaymentAllocationDto(Guid Id, Guid APInvoiceId, decimal AllocatedAmount);

public sealed record PaymentDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid VendorId,
    Guid BankAccountId,
    DateOnly PaymentDate,
    string PaymentMethod,
    string? Reference,
    IReadOnlyList<PaymentAllocationDto> Allocations,
    decimal Amount,
    Guid? LinkedJournalEntryId,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreatePaymentAllocationRequest(Guid APInvoiceId, decimal AllocatedAmount);

public sealed record CreatePaymentRequest(
    Guid VendorId,
    Guid BankAccountId,
    DateOnly PaymentDate,
    string PaymentMethod,
    IReadOnlyList<CreatePaymentAllocationRequest> Allocations,
    string? Reference = null);
