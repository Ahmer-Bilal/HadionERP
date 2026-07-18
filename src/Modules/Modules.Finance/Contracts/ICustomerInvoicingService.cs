namespace Modules.Finance.Contracts;

/// <summary>
/// What another module needs to supply to have Finance raise a real AR Invoice on its behalf — the fields
/// mirror <c>Modules.Finance.Application.CreateARInvoiceRequest</c> exactly, since that's genuinely all Finance
/// needs; this Contracts-package record exists only so the calling module never references Finance's own
/// Application-layer DTO directly (docs/architecture/01-overview.md §3.2 rule 2).
/// </summary>
public sealed record RaiseCustomerInvoiceRequest(
    Guid CustomerId,
    string? CustomerReference,
    DateOnly InvoiceDate,
    string Description,
    Guid RevenueAccountId,
    Guid ReceivableAccountId,
    decimal NetAmount,
    Guid? CostCenterId,
    Guid? TaxCodeId,
    Guid? VatAccountId,
    // What actually raised this invoice from the caller's own perspective — "Ipc" or "RetentionRelease"
    // today, more as Construction grows. Flows straight through to ARInvoice.SourceDocumentType, the same
    // "what created this" trace JournalEntry.SourceDocumentType already established one level down the
    // chain (see that field's own doc comment).
    string SourceDocumentType,
    Guid SourceDocumentId);

/// <summary>
/// The first cross-module *write* Contracts interface in this system (every other Contracts interface so
/// far — <see cref="IAPInvoiceLookup"/>, <see cref="IBudgetCheckService"/>, <c>IProjectLookup</c>,
/// <c>IBusinessPartnerLookup</c> — is read-only). Construction calls this when an IPC against a Contract
/// (never a Subcontract — that side represents a payable to a subcontractor, a separate, not-yet-built AP
/// integration) is certified, raising a real AR Invoice in Draft rather than leaving Finance to remember to
/// create one manually. Deliberately returns only the new invoice's id, not the full document — the caller
/// (Construction) has no legitimate reason to see AR Invoice internals beyond "it was created, here's the
/// id to keep for traceability," the same "expose exactly what's needed, nothing more" discipline every
/// other Contracts interface in this system already follows. Implemented in Modules.Finance.Infrastructure,
/// registered in Gateway.Api's DI container.
/// </summary>
public interface ICustomerInvoicingService
{
    Task<Guid> RaiseInvoiceAsync(
        RaiseCustomerInvoiceRequest request, string actor, string companyId, CancellationToken cancellationToken = default);
}
