namespace Modules.Procurement.Application;

/// <summary>
/// The result of comparing what was Ordered (<c>PurchaseOrder.Total</c>), what was Received (the sum of
/// every Approved <see cref="Domain.GoodsReceiptNote"/> against that PO), and what was Invoiced (a Finance
/// AP Invoice's <c>NetAmount</c>, looked up via <c>Modules.Finance.Contracts.IAPInvoiceLookup</c>) — the
/// "3-way match" docs/architecture/06-roadmap.md's Phase 2 bullet names. Computed on demand, never
/// persisted — same "computed, not stored" reasoning as every other module's running total
/// (<c>PurchaseOrder.Total</c>/<c>PurchaseRequisition.EstimatedTotal</c> included).
/// </summary>
public sealed record ThreeWayMatchResult(
    Guid PurchaseOrderId,
    Guid ApInvoiceId,
    bool VendorMatches,
    decimal OrderedTotal,
    decimal ReceivedValue,
    decimal InvoicedNetAmount,
    bool WithinReceived,
    bool WithinOrdered,
    bool IsMatched,
    IReadOnlyList<string> VarianceNotes);
