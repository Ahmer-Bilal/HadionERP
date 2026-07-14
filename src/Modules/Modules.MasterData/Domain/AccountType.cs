namespace Modules.MasterData.Domain;

/// <summary>
/// The five classical accounting account types. Determines financial-statement placement (Asset/Liability/
/// Equity → Balance Sheet; Revenue/Expense → Income Statement) and the account's normal balance side
/// (Asset/Expense → Debit-normal; Liability/Equity/Revenue → Credit-normal). This is the standard SAP/
/// Dynamics account-type taxonomy; contra-account overrides and more granular sub-types (e.g. SAP's primary/
/// secondary accounts) are deferred — these five are sufficient for the Phase 1 Universal Journal.
/// </summary>
public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
}
