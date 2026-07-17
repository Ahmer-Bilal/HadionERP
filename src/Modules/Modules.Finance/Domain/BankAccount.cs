using Platform.Core;

namespace Modules.Finance.Domain;

/// <summary>
/// One bank account this company holds — SAP's "House Bank" / Dynamics's "Bank account" concept
/// (`MISSING-FEATURES-AUDIT.md` Part 2 §16). A <see cref="Payment"/> credits a real bank account's
/// <see cref="LinkedGLAccountId"/> when posted, rather than a payment guessing at a single hardcoded "Cash"
/// account — a real company routes payments through several accounts (main operating account, a project-
/// specific account, etc.).
///
/// Deliberately flat, no parent hierarchy, mirroring <see cref="TaxCode"/>'s shape — a bank account list
/// doesn't need roll-ups. Follows the standard Business Object lifecycle (Draft → Submit → Approve) since
/// a wrong or duplicate bank account pollutes every payment that references it afterward, the same control-
/// point reasoning as every other Phase 1 master-data entity. Stops at Approved — a bank account is master
/// data, not a financial document itself, so there's no Post/Reverse.
/// </summary>
public sealed class BankAccount : BusinessObject
{
    /// <summary>The business-facing account code (e.g. "BANK-SAR-01"), distinct from the sequential
    /// <see cref="BusinessObject.DocumentNumber"/> audit id. Must be unique — enforced by the service + a
    /// DB unique index.</summary>
    public string AccountCode { get; private set; }

    public string AccountName { get; private set; }

    /// <summary>The account's name in Arabic — same bilingual precedent as every other Phase 1 master-data
    /// entity.</summary>
    public string? AccountNameArabic { get; private set; }

    public string BankName { get; private set; }

    public string? Iban { get; private set; }

    /// <summary>The real Asset/Bank G/L account a <see cref="Payment"/> against this bank account credits
    /// when posted — validated `IsPostable`/`IsActive` at the Application layer the same way
    /// `APInvoiceService.ValidateAccountAsync` already validates `APInvoice.ExpenseAccountId`/
    /// `PayableAccountId`, not resolved from a guessed default.</summary>
    public Guid LinkedGLAccountId { get; private set; }

    /// <summary>True when the account accepts new payments. Deactivating (rather than deleting) a bank
    /// account that already has payment history keeps prior payments valid while preventing new ones — the
    /// same "correct by reversal, not by deletion" principle used everywhere else in this platform.</summary>
    public bool IsActive { get; private set; }

    public BankAccount(string createdBy, string accountCode, string accountName, string bankName, Guid linkedGLAccountId)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName);

        AccountCode = accountCode;
        AccountName = accountName;
        BankName = bankName;
        LinkedGLAccountId = linkedGLAccountId;
        IsActive = true;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private BankAccount()
    {
        AccountCode = null!;
        AccountName = null!;
        BankName = null!;
    }

    public void UpdateAccountName(string accountName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        AccountName = accountName;
    }

    public void UpdateAccountNameArabic(string? accountNameArabic) => AccountNameArabic = accountNameArabic;

    public void UpdateBankName(string bankName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName);
        BankName = bankName;
    }

    public void UpdateIban(string? iban) => Iban = iban;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
