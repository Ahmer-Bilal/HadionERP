using Platform.Core;

namespace Modules.MasterData.Domain;

/// <summary>
/// One General Ledger account — a row in the Chart of Accounts. This is the "GL Account" dimension carried
/// on every Journal Line Item (docs/architecture/07-project-accounting-and-financial-architecture.md #1),
/// and one of the two master-data pieces the Phase 1 exit criteria explicitly names ("maintain its chart of
/// accounts and vendors"). Follows the standard Business Object lifecycle (Platform.Core.BusinessObject):
/// Draft → Submit → Approve → Approved is the "active, usable" state, same as Business Partner.
///
/// The chart is a hierarchy: an account may have an optional <see cref="ParentAccountId"/> (e.g. "Current
/// Assets" groups "Cash", "Petty Cash", "Bank — SAR"). Grouping/header accounts are marked
/// <see cref="IsPostable"/>=false so journal lines can only touch leaf accounts, never a roll-up node.
/// </summary>
public sealed class GLAccount : BusinessObject
{
    /// <summary>The business-facing chart code the accountant assigns (e.g. "1010", "2-2010"), distinct from
    /// the sequential <see cref="BusinessObject.DocumentNumber"/> (an internal audit id like
    /// "MD-GL-2026-000001"). Must be unique across the chart — enforced by the service + a DB unique index.</summary>
    public string AccountCode { get; private set; }

    public string AccountName { get; private set; }

    /// <summary>The account's name in Arabic — for a correctly localized Arabic UI and financial statements,
    /// same precedent as <see cref="BusinessPartner.NameArabic"/>. Account names are data (the business's
    /// own chart), not translatable copy, so they're entered rather than machine-translated.</summary>
    public string? AccountNameArabic { get; private set; }

    public AccountType AccountType { get; private set; }

    /// <summary>Optional parent account for the grouping hierarchy (e.g. "Current Assets" → "Cash"). Null
    /// for a top-level account. Self-referencing.</summary>
    public Guid? ParentAccountId { get; private set; }

    /// <summary>True for a leaf account journal lines can post to; false for a header/grouping account used
    /// only to structure the chart. Prevents posting to a roll-up node.</summary>
    public bool IsPostable { get; private set; }

    /// <summary>True when the account accepts new postings. Deactivating (rather than deleting) an account
    /// that already has history keeps prior postings valid while preventing new ones — the same "correct by
    /// reversal, not by deletion" principle as the platform's hard-delete rule for posted documents.</summary>
    public bool IsActive { get; private set; }

    public GLAccount(string createdBy, string accountCode, string accountName, AccountType accountType)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        AccountCode = accountCode;
        AccountName = accountName;
        AccountType = accountType;
        IsPostable = true;
        IsActive = true;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private GLAccount()
    {
        AccountCode = null!;
        AccountName = null!;
    }

    public void UpdateAccountName(string accountName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        AccountName = accountName;
    }

    public void UpdateAccountNameArabic(string? accountNameArabic) => AccountNameArabic = accountNameArabic;

    public void AssignParent(Guid? parentAccountId) => ParentAccountId = parentAccountId;

    public void SetPostable(bool isPostable) => IsPostable = isPostable;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    /// <summary>The normal balance side derived from <see cref="AccountType"/> — Asset/Expense are
    /// debit-normal, Liability/Equity/Revenue are credit-normal. Not stored: deriving it keeps a single
    /// source of truth (the type) and avoids the contra-account-table duplication. Contra overrides are
    /// deferred (advanced).</summary>
    public string NormalBalance => AccountType is AccountType.Asset or AccountType.Expense ? "Debit" : "Credit";

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
