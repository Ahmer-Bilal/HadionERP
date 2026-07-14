using Platform.Security;
using Platform.Security.Sod;

namespace Modules.MasterData.Application;

/// <summary>
/// The Privileges/Duties/Roles/SoD conflict rule Cost Center registers into Platform.Security at startup —
/// same module-owned security configuration pattern as <see cref="GLAccountSecurity"/> and
/// <see cref="ItemSecurity"/>. Maintainer (create, update, submit) and Approver (approve/reject) are split
/// for the same reason: adding a miscoded/duplicate cost center to the master is a real control point.
/// </summary>
public static class CostCenterSecurity
{
    public const string MaintainPrivilegeKey = "MasterData.CostCenter.Maintain";
    public const string ApprovePrivilegeKey = "MasterData.CostCenter.Approve";

    public const string MaintainerDutyKey = "MasterData.CostCenter.Maintainer";
    public const string ApproverDutyKey = "MasterData.CostCenter.Approver";

    public const string MaintainerRoleKey = "MasterData.CostCenter.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Cost Centers (create, update, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Cost Center",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Cost Center maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        CostCenterWorkflow.ApproverRoleKey, "Cost Center approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve a Cost Center " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2).");
}
