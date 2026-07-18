using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Finance.Application;

/// <summary>
/// The Privileges/Duties/Roles/SoD conflict rule Budget registers into Platform.Security at startup — same
/// module-owned security configuration pattern as <see cref="JournalEntrySecurity"/>. Splitting Maintainer
/// (create/submit) from Approver (approve/reject) matters more here than almost anywhere else: the person
/// requesting a spending ceiling should never be the same person who grants it.
/// </summary>
public static class BudgetSecurity
{
    public const string MaintainPrivilegeKey = "Finance.Budget.Maintain";
    public const string ApprovePrivilegeKey = "Finance.Budget.Approve";

    public const string MaintainerDutyKey = "Finance.Budget.Maintainer";
    public const string ApproverDutyKey = "Finance.Budget.Approver";

    public const string MaintainerRoleKey = "Finance.Budget.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Budgets (create, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Budget",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Budget maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        BudgetWorkflow.ApproverRoleKey, "Budget approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve a Budget " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
