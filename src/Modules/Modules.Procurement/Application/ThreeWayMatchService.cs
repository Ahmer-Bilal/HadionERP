using Modules.Finance.Contracts;
using Platform.Core;

namespace Modules.Procurement.Application;

/// <summary>
/// Computes the 3-way match (Ordered vs Received vs Invoiced) docs/architecture/06-roadmap.md's Phase 2
/// bullet names as the last piece of the procure-to-pay chain — "PR → RFQ → PO → GRN → 3-way match against
/// AP." Deliberately lives entirely on the Procurement side: Finance is upstream of Procurement in the
/// module dependency graph (docs/architecture/01-architecture-foundation.md §3.2 — Procurement depends on
/// Finance, never the reverse), so this reads the PO/GRN data it already owns directly and reaches into
/// Finance only through the one published, read-only <see cref="IAPInvoiceLookup"/> contract — the same
/// direction <see cref="PurchaseOrderService"/> already uses for the budget check.
///
/// The match is at the document-amount level (Ordered/Received/Invoiced totals), not line-by-line — a real
/// line-by-line match would need <c>APInvoice</c> to carry lines referencing PO lines, which it doesn't
/// (APInvoice is header/amount-only, by design — see its own doc comment); reworking that shape is a bigger
/// change than this Phase 2 slice justifies, and is disclosed as deferred rather than attempted here.
/// </summary>
public sealed class ThreeWayMatchService
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IGoodsReceiptNoteRepository _goodsReceiptNoteRepository;
    private readonly IAPInvoiceLookup _apInvoiceLookup;

    public ThreeWayMatchService(
        IPurchaseOrderRepository purchaseOrderRepository,
        IGoodsReceiptNoteRepository goodsReceiptNoteRepository,
        IAPInvoiceLookup apInvoiceLookup)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptNoteRepository = goodsReceiptNoteRepository;
        _apInvoiceLookup = apInvoiceLookup;
    }

    public async Task<ThreeWayMatchResult> CheckAsync(Guid purchaseOrderId, Guid apInvoiceId, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetAsync(purchaseOrderId, cancellationToken)
            ?? throw new ArgumentException($"Purchase order {purchaseOrderId} was not found.");
        var invoice = await _apInvoiceLookup.GetAsync(apInvoiceId, cancellationToken)
            ?? throw new ArgumentException($"AP invoice {apInvoiceId} was not found.");

        var grns = await _goodsReceiptNoteRepository.ListByPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        var receivedValue = grns.Where(g => g.Status == BusinessObjectStatus.Approved).Sum(g => g.ReceivedValue);

        var vendorMatches = invoice.VendorId == purchaseOrder.VendorId;
        var withinReceived = invoice.NetAmount <= receivedValue;
        var withinOrdered = invoice.NetAmount <= purchaseOrder.Total;

        var notes = new List<string>();
        if (!vendorMatches)
            notes.Add("The invoice's vendor does not match the purchase order's vendor.");
        if (!withinReceived)
            notes.Add($"Invoiced amount {invoice.NetAmount} exceeds received value {receivedValue} — goods for this amount have not all been received (or receipt not yet Approved).");
        if (!withinOrdered)
            notes.Add($"Invoiced amount {invoice.NetAmount} exceeds the purchase order's total {purchaseOrder.Total}.");

        var isMatched = vendorMatches && withinReceived && withinOrdered;

        return new ThreeWayMatchResult(
            purchaseOrderId, apInvoiceId, vendorMatches, purchaseOrder.Total, receivedValue, invoice.NetAmount,
            withinReceived, withinOrdered, isMatched, notes);
    }
}
