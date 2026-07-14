using Platform.Security;
using Platform.Security.Sod;

namespace Modules.MasterData.Application;

/// <summary>
/// The Privileges/Duties/Roles/SoD conflict rule Item registers into Platform.Security at startup — same
/// module-owned security configuration pattern as <see cref="BusinessPartnerSecurity"/> and
/// <see cref="GLAccountSecurity"/>. Maintainer (create, update, submit) and Approver (approve/reject) are
/// split for the same reason: adding a miscoded/duplicate item to the master is a real control point.
/// </summary>
public static class ItemSecurity
{
    public const string MaintainPrivilegeKey = "MasterData.Item.Maintain";
    public const string ApprovePrivilegeKey = "MasterData.Item.Approve";

    public const string MaintainerDutyKey = "MasterData.Item.Maintainer";
    public const string ApproverDutyKey = "MasterData.Item.Approver";

    public const string MaintainerRoleKey = "MasterData.Item.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Items (create, update, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject an Item",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Item maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        ItemWorkflow.ApproverRoleKey, "Item approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve an Item " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2).");
}
