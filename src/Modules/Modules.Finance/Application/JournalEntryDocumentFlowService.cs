namespace Modules.Finance.Application;

/// <summary>
/// Resolves a Journal Entry's real document flow (`UI/Finance/Finance_Jornal Entry_Object.png`'s "Document
/// Flow" rail) — Source Document → this Journal Entry → Reversal (if any) → Settlement (Payment/Customer
/// Receipt, if the source is an invoice). Deliberately stops at Finance's own module boundary: an AP/AR
/// Invoice's own <c>SourceDocumentType</c> (e.g. <c>"Ipc"</c>, <c>"RetentionRelease"</c>,
/// <c>"PurchaseOrder"</c>) is returned as a raw type+id pair, never resolved to a friendly document
/// number/status here — that would mean Finance reading Construction's or Procurement's own data, which
/// `docs/architecture/01-overview.md` §3.2's module graph forbids (Finance is upstream of both; the
/// dependency only ever runs the other way). The frontend resolves that one extra hop itself, calling
/// Construction's/Procurement's own already-published APIs directly — the same "compose the chain in the
/// client, not the server" answer this session already gave for the Purchase Order → RFQ → Requisition and
/// → Goods Receipt legs of the same panel.
/// </summary>
public sealed class JournalEntryDocumentFlowService
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IAPInvoiceRepository _apInvoiceRepository;
    private readonly IARInvoiceRepository _arInvoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICustomerReceiptRepository _customerReceiptRepository;

    public JournalEntryDocumentFlowService(
        IJournalEntryRepository journalEntryRepository,
        IAPInvoiceRepository apInvoiceRepository,
        IARInvoiceRepository arInvoiceRepository,
        IPaymentRepository paymentRepository,
        ICustomerReceiptRepository customerReceiptRepository)
    {
        _journalEntryRepository = journalEntryRepository;
        _apInvoiceRepository = apInvoiceRepository;
        _arInvoiceRepository = arInvoiceRepository;
        _paymentRepository = paymentRepository;
        _customerReceiptRepository = customerReceiptRepository;
    }

    public async Task<IReadOnlyList<JournalEntryDocumentFlowNodeDto>> GetAsync(Guid journalEntryId, CancellationToken cancellationToken = default)
    {
        var entry = await _journalEntryRepository.GetAsync(journalEntryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Journal entry {journalEntryId} was not found.");

        var nodes = new List<JournalEntryDocumentFlowNodeDto>();

        if (entry.SourceDocumentType is { } sourceType && sourceType != JournalEntrySourceDocumentTypes.Manual
            && entry.SourceDocumentId is { } sourceId)
        {
            switch (sourceType)
            {
                case JournalEntrySourceDocumentTypes.APInvoice:
                    var apInvoice = await _apInvoiceRepository.GetAsync(sourceId, cancellationToken);
                    if (apInvoice is not null)
                    {
                        // One extra hop, still Finance's own module: what raised the invoice itself.
                        if (apInvoice.SourceDocumentType is { } apOrigin && apOrigin != "Manual" && apInvoice.SourceDocumentId is { } apOriginId)
                            nodes.Add(new JournalEntryDocumentFlowNodeDto(apOrigin, apOrigin, null, apOriginId, "Unknown", IsCurrent: false));

                        nodes.Add(new JournalEntryDocumentFlowNodeDto(
                            "APInvoice", "Accounts Payable Invoice", apInvoice.DocumentNumber, apInvoice.Id, apInvoice.Status.ToString(), IsCurrent: false));
                    }
                    break;

                case JournalEntrySourceDocumentTypes.ARInvoice:
                    var arInvoice = await _arInvoiceRepository.GetAsync(sourceId, cancellationToken);
                    if (arInvoice is not null)
                    {
                        if (arInvoice.SourceDocumentType is { } arOrigin && arOrigin != "Manual" && arInvoice.SourceDocumentId is { } arOriginId)
                            nodes.Add(new JournalEntryDocumentFlowNodeDto(arOrigin, arOrigin, null, arOriginId, "Unknown", IsCurrent: false));

                        nodes.Add(new JournalEntryDocumentFlowNodeDto(
                            "ARInvoice", "Accounts Receivable Invoice", arInvoice.DocumentNumber, arInvoice.Id, arInvoice.Status.ToString(), IsCurrent: false));
                    }
                    break;

                case JournalEntrySourceDocumentTypes.Payment:
                    var payment = await _paymentRepository.GetAsync(sourceId, cancellationToken);
                    if (payment is not null)
                        nodes.Add(new JournalEntryDocumentFlowNodeDto(
                            "Payment", "Payment", payment.DocumentNumber, payment.Id, payment.Status.ToString(), IsCurrent: false));
                    break;

                case JournalEntrySourceDocumentTypes.CustomerReceipt:
                    var receipt = await _customerReceiptRepository.GetAsync(sourceId, cancellationToken);
                    if (receipt is not null)
                        nodes.Add(new JournalEntryDocumentFlowNodeDto(
                            "CustomerReceipt", "Customer Receipt", receipt.DocumentNumber, receipt.Id, receipt.Status.ToString(), IsCurrent: false));
                    break;
            }
        }

        if (entry.ReversalOfEntryId is { } originalId)
        {
            var original = await _journalEntryRepository.GetAsync(originalId, cancellationToken);
            if (original is not null)
                nodes.Add(new JournalEntryDocumentFlowNodeDto(
                    "Reversal", "Reverses", original.DocumentNumber, original.Id, original.Status.ToString(), IsCurrent: false));
        }

        nodes.Add(new JournalEntryDocumentFlowNodeDto(
            "JournalEntry", "Journal Entry", entry.DocumentNumber, entry.Id, entry.Status.ToString(), IsCurrent: true));

        var reversedBy = await _journalEntryRepository.FindReversalOfAsync(entry.Id, cancellationToken);
        if (reversedBy is not null)
            nodes.Add(new JournalEntryDocumentFlowNodeDto(
                "Reversal", "Reversed By", reversedBy.DocumentNumber, reversedBy.Id, reversedBy.Status.ToString(), IsCurrent: false));

        // Settlement: only meaningful once the entry (and its source invoice, if any) is Posted — an
        // unposted invoice can't legitimately have a payment against it yet.
        if (entry.SourceDocumentType == JournalEntrySourceDocumentTypes.APInvoice && entry.SourceDocumentId is { } apId)
        {
            var payments = await _paymentRepository.ListByInvoiceAsync(apId, cancellationToken);
            var postedPayment = payments.FirstOrDefault(p => p.Status == Platform.Core.BusinessObjectStatus.Posted);
            nodes.Add(postedPayment is not null
                ? new JournalEntryDocumentFlowNodeDto("Payment", "Payment", postedPayment.DocumentNumber, postedPayment.Id, "Posted", IsCurrent: false)
                : new JournalEntryDocumentFlowNodeDto("Payment", "Payment", null, null, "Pending", IsCurrent: false));
        }
        else if (entry.SourceDocumentType == JournalEntrySourceDocumentTypes.ARInvoice && entry.SourceDocumentId is { } arId)
        {
            var receipts = await _customerReceiptRepository.ListByInvoiceAsync(arId, cancellationToken);
            var postedReceipt = receipts.FirstOrDefault(r => r.Status == Platform.Core.BusinessObjectStatus.Posted);
            nodes.Add(postedReceipt is not null
                ? new JournalEntryDocumentFlowNodeDto("CustomerReceipt", "Customer Receipt", postedReceipt.DocumentNumber, postedReceipt.Id, "Posted", IsCurrent: false)
                : new JournalEntryDocumentFlowNodeDto("CustomerReceipt", "Customer Receipt", null, null, "Pending", IsCurrent: false));
        }

        return nodes;
    }
}
