using Platform.Core;

namespace Modules.Finance.Domain;

/// <summary>
/// One Accounts Receivable invoice — the customer-billing mirror of <see cref="APInvoice"/>, closing
/// `docs/module/finance.md`'s own "Accounts Payable and Accounts Receivable as their own document types...
/// is designed but not yet built" gap on the AR side. Same Draft → Submit → Approve → Post → Reverse
/// lifecycle; Posting generates a real linked <see cref="JournalEntry"/> (Dr Receivable, Cr Revenue[/VAT
/// Output]) rather than just changing a status — see <c>ARInvoiceService.PostAsync</c>. The VAT side is the
/// mirror image of AP's: AP debits VAT Input (recoverable from the government), AR credits VAT Output
/// (payable to the government) — same tax rate mechanics, opposite ledger direction.
///
/// Deliberately requires the poster to pick the Revenue and Receivable ("AR control") accounts explicitly,
/// same reasoning as <see cref="APInvoice"/> — no admin configuration UI exists yet to set safe defaults.
/// <see cref="TaxRate"/> is a snapshot of the referenced Tax Code's rate at creation time, not a live
/// reference, same "freeze financial facts at the moment of the transaction" pattern.
///
/// Deliberately NOT yet wired to Construction's IPC — PROGRESS.md's 2026-07-16 entry explicitly left
/// "does IPC certification raise an AR invoice immediately, or a WIP/unbilled-revenue step first" as an
/// open decision, not guessed here. This type exists as the standalone AR capability that decision will
/// eventually call into, the same way APInvoice existed as a standalone capability before Procurement's
/// three-way match called into it.
/// </summary>
public sealed class ARInvoice : BusinessObject
{
    public Guid CustomerId { get; private set; }

    /// <summary>The customer's own reference for this invoice — their PO number, or an IPC reference once
    /// that integration exists — genuinely optional, unlike <see cref="APInvoice.VendorInvoiceNumber"/>,
    /// since this platform is the one issuing the invoice, not recording someone else's.</summary>
    public string? CustomerReference { get; private set; }

    public DateOnly InvoiceDate { get; private set; }

    public string Description { get; private set; }

    public Guid RevenueAccountId { get; private set; }

    public Guid ReceivableAccountId { get; private set; }

    public Guid? CostCenterId { get; private set; }

    public Guid? TaxCodeId { get; private set; }

    /// <summary>Snapshot of the Tax Code's rate at creation time — see the class doc comment. Zero when
    /// no Tax Code was referenced.</summary>
    public decimal TaxRate { get; private set; }

    /// <summary>Required once <see cref="TaxRate"/> is non-zero (validated by
    /// <c>ARInvoiceService.CreateAsync</c>) — the account VAT output is credited to.</summary>
    public Guid? VatAccountId { get; private set; }

    public decimal NetAmount { get; private set; }

    public decimal TaxAmount => Math.Round(NetAmount * TaxRate / 100m, 2);

    public decimal GrossAmount => NetAmount + TaxAmount;

    /// <summary>Set once, when this invoice is Posted — the G/L Journal Entry generated for it. Reversing
    /// this invoice reverses that linked entry too (see <c>ARInvoiceService.ReverseAsync</c>).</summary>
    public Guid? LinkedJournalEntryId { get; private set; }

    /// <summary>What actually raised this invoice — <c>"Manual"</c>, <c>"Ipc"</c> (a Contract-type IPC's
    /// certification), or <c>"RetentionRelease"</c> (a Contract-type retention release). Mirrors
    /// <see cref="APInvoice.SourceDocumentType"/> exactly — see that field's own doc comment.</summary>
    public string? SourceDocumentType { get; private set; }

    public Guid? SourceDocumentId { get; private set; }

    public ARInvoice(
        string createdBy, Guid customerId, string? customerReference, DateOnly invoiceDate, string description,
        Guid revenueAccountId, Guid receivableAccountId, decimal netAmount)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (netAmount <= 0)
            throw new ArgumentException("Net amount must be positive.", nameof(netAmount));

        CustomerId = customerId;
        CustomerReference = customerReference;
        InvoiceDate = invoiceDate;
        Description = description;
        RevenueAccountId = revenueAccountId;
        ReceivableAccountId = receivableAccountId;
        NetAmount = netAmount;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private ARInvoice()
    {
        Description = null!;
    }

    public void SetCostCenter(Guid? costCenterId) => CostCenterId = costCenterId;

    public void SetTax(Guid taxCodeId, decimal taxRate, Guid vatAccountId)
    {
        if (taxRate < 0 || taxRate > 100)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate must be between 0 and 100.");
        TaxCodeId = taxCodeId;
        TaxRate = taxRate;
        VatAccountId = vatAccountId;
    }

    public void LinkJournalEntry(Guid journalEntryId) => LinkedJournalEntryId = journalEntryId;

    public void MarkSourceDocument(string sourceDocumentType, Guid? sourceDocumentId)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("The source document can only be set while the invoice is in Draft.");
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDocumentType);
        SourceDocumentType = sourceDocumentType;
        SourceDocumentId = sourceDocumentId;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);

    public void Post(string actor) => Transition(BusinessObjectTransition.Post, actor);

    public void Reverse(string actor) => Transition(BusinessObjectTransition.Reverse, actor);
}
