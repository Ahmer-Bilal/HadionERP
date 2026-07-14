using Platform.Core;

namespace Modules.Finance.Domain;

/// <summary>
/// One Accounts Payable invoice — the second Finance Business Object, and the other half of the Phase 1
/// exit criteria: "post/reverse a GL journal and an AP invoice end-to-end with full audit trail"
/// (docs/architecture/06-roadmap.md). Same Draft → Submit → Approve → Post → Reverse lifecycle as
/// <see cref="JournalEntry"/>; Posting an invoice generates a real linked <see cref="JournalEntry"/>
/// (Dr Expense[/VAT], Cr Payable) rather than just changing a status — see
/// <c>APInvoiceService.PostAsync</c>.
///
/// Deliberately requires the poster to pick the Expense account, the Payable ("AP control") account, and
/// (if tax applies) the VAT account explicitly on each invoice, the same way a Journal Entry line's G/L
/// Account is explicitly chosen — rather than resolving a "default AP control account" from configuration.
/// A real AP process usually configures those defaults once, but there is no admin configuration UI in
/// this application yet to set them safely (Platform.Configuration's override mechanism exists, but a
/// real G/L Account id is only known at runtime, not at compile time as a registered default) — disclosed
/// as deferred in this module's README rather than seeding a fake default no admin actually set.
///
/// <see cref="TaxRate"/> is a snapshot of the referenced Tax Code's rate at creation time, not a live
/// reference — the classic "freeze financial facts at the moment of the transaction" pattern, so a later
/// change to the tax code's rate never retroactively changes an already-created invoice.
/// </summary>
public sealed class APInvoice : BusinessObject
{
    public Guid VendorId { get; private set; }

    /// <summary>The vendor's own invoice reference/number — distinct from
    /// <see cref="BusinessObject.DocumentNumber"/>, this platform's own sequential audit id.</summary>
    public string VendorInvoiceNumber { get; private set; }

    public DateOnly InvoiceDate { get; private set; }

    public string Description { get; private set; }

    public Guid ExpenseAccountId { get; private set; }

    public Guid PayableAccountId { get; private set; }

    public Guid? CostCenterId { get; private set; }

    public Guid? TaxCodeId { get; private set; }

    /// <summary>Snapshot of the Tax Code's rate at creation time — see the class doc comment. Zero when
    /// no Tax Code was referenced.</summary>
    public decimal TaxRate { get; private set; }

    /// <summary>Required once <see cref="TaxRate"/> is non-zero (validated by
    /// <c>APInvoiceService.CreateAsync</c>, since the Domain constructor doesn't have lookup access to
    /// check accounts) — the account VAT input is debited to.</summary>
    public Guid? VatAccountId { get; private set; }

    public decimal NetAmount { get; private set; }

    public decimal TaxAmount => Math.Round(NetAmount * TaxRate / 100m, 2);

    public decimal GrossAmount => NetAmount + TaxAmount;

    /// <summary>Set once, when this invoice is Posted — the G/L Journal Entry generated for it. Reversing
    /// this invoice reverses that linked entry too (see <c>APInvoiceService.ReverseAsync</c>).</summary>
    public Guid? LinkedJournalEntryId { get; private set; }

    public APInvoice(
        string createdBy, Guid vendorId, string vendorInvoiceNumber, DateOnly invoiceDate, string description,
        Guid expenseAccountId, Guid payableAccountId, decimal netAmount)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vendorInvoiceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (netAmount <= 0)
            throw new ArgumentException("Net amount must be positive.", nameof(netAmount));

        VendorId = vendorId;
        VendorInvoiceNumber = vendorInvoiceNumber;
        InvoiceDate = invoiceDate;
        Description = description;
        ExpenseAccountId = expenseAccountId;
        PayableAccountId = payableAccountId;
        NetAmount = netAmount;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private APInvoice()
    {
        VendorInvoiceNumber = null!;
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

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);

    public void Post(string actor) => Transition(BusinessObjectTransition.Post, actor);

    public void Reverse(string actor) => Transition(BusinessObjectTransition.Reverse, actor);
}
