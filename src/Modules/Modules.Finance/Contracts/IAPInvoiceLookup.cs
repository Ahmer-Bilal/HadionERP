namespace Modules.Finance.Contracts;

/// <summary>
/// The published, read-only view of an AP Invoice another module (Procurement, for the 3-way match check)
/// may depend on — same Contracts-package rule as <see cref="IBudgetCheckService"/>. Procurement needs to
/// know "who is this billed to and how much," not APInvoice's own posting/reversal mechanics.
/// </summary>
public sealed record APInvoiceSummary(
    Guid Id,
    string? DocumentNumber,
    Guid VendorId,
    string Status,
    decimal NetAmount,
    decimal GrossAmount);

/// <summary>Read-only lookup Procurement calls for the 3-way match check (Ordered vs Received vs Invoiced) —
/// implemented in Modules.Finance.Infrastructure, registered in Gateway.Api's DI container. Same dependency
/// direction as <see cref="IBudgetCheckService"/>: Procurement depends on this published Contracts package,
/// Finance never depends on Procurement (docs/architecture/01-architecture-foundation.md §3.2's module
/// graph — Finance is upstream of Procurement).</summary>
public interface IAPInvoiceLookup
{
    Task<APInvoiceSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
