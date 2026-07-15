using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Security configuration pattern as <see cref="APInvoiceSecurity"/> — a
/// deliberately separate Duty/Role pair, since maintaining bank account master data and approving it are
/// different real-world authorities (a wrong bank account routes real money to the wrong place).</summary>
public static class BankAccountSecurity
{
    public const string MaintainPrivilegeKey = "Finance.BankAccount.Maintain";
    public const string ApprovePrivilegeKey = "Finance.BankAccount.Approve";

    public const string MaintainerDutyKey = "Finance.BankAccount.Maintainer";
    public const string ApproverDutyKey = "Finance.BankAccount.Approver";

    public const string MaintainerRoleKey = "Finance.BankAccount.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Bank Accounts (create, update, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Bank Account",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Bank Account maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        BankAccountWorkflow.ApproverRoleKey, "Bank Account approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve a Bank Account " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2).");
}
