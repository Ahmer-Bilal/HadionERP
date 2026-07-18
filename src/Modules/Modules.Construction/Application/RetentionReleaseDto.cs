namespace Modules.Construction.Application;

public sealed record RetentionReleaseDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid ProjectId,
    string CommercialDocumentType,
    Guid CommercialDocumentId,
    DateOnly ReleaseDate,
    decimal AmountReleased,
    string TriggerEvent,
    Guid? RevenueAccountId,
    Guid? ReceivableAccountId,
    Guid? ExpenseAccountId,
    Guid? PayableAccountId,
    Guid? TaxCodeId,
    Guid? VatAccountId,
    Guid? LinkedArInvoiceId,
    Guid? LinkedApInvoiceId,
    DateTimeOffset CreatedAt,
    string CreatedBy);

/// <summary>The running retention balance for one commercial document (construction-commercial-processes-
/// spec.md §5) — every Approved <c>Ipc</c>'s own <c>RetentionAmount</c> summed, less every Approved
/// <see cref="RetentionRelease"/>'s <see cref="RetentionReleaseDto.AmountReleased"/> already released.
/// <see cref="OutstandingBalance"/> is what a new release's <c>AmountReleased</c> is validated against.</summary>
public sealed record RetentionBalanceDto(
    Guid CommercialDocumentId, decimal TotalWithheldToDate, decimal TotalReleasedToDate, decimal OutstandingBalance);

/// <summary><see cref="RevenueAccountId"/>/<see cref="ReceivableAccountId"/> are required when
/// <see cref="CommercialDocumentType"/> is Contract (raises an AR Invoice); <see cref="ExpenseAccountId"/>/
/// <see cref="PayableAccountId"/> are required when it's Subcontract (raises an AP Invoice) — exactly one
/// pair is ever populated, same rule <c>CreateIpcRequest</c> already enforces.</summary>
public sealed record CreateRetentionReleaseRequest(
    Guid ProjectId, string CommercialDocumentType, Guid CommercialDocumentId,
    DateOnly ReleaseDate, decimal AmountReleased, string TriggerEvent,
    Guid? RevenueAccountId = null, Guid? ReceivableAccountId = null, Guid? TaxCodeId = null, Guid? VatAccountId = null,
    Guid? ExpenseAccountId = null, Guid? PayableAccountId = null);
