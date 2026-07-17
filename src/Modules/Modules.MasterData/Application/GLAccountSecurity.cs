using Platform.Security;
using Platform.Security.Sod;

namespace Modules.MasterData.Application;

/// <summary>
/// The Privileges/Duties/Roles/SoD conflict rule G/L Account registers into Platform.Security at startup —
/// the same module-owned security configuration pattern as <see cref="BusinessPartnerSecurity"/>. Maintainer
/// (create, update, submit) and Approver (approve/reject) are deliberately split — changing the chart of
/// accounts is a financial control point, not open to the same person who approves it.
/// </summary>
public static class GLAccountSecurity
{
    public const string MaintainPrivilegeKey = "MasterData.GLAccount.Maintain";
    public const string ApprovePrivilegeKey = "MasterData.GLAccount.Approve";

    public const string MaintainerDutyKey = "MasterData.GLAccount.Maintainer";
    public const string ApproverDutyKey = "MasterData.GLAccount.Approver";

    public const string MaintainerRoleKey = "MasterData.GLAccount.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain G/L Accounts (create, update, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a G/L Account",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "G/L Account maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        GLAccountWorkflow.ApproverRoleKey, "G/L Account approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve a G/L Account " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
