using Platform.Security;

namespace Modules.Identity.Application;

/// <summary>
/// The Privilege/Duty/Role Identity registers into Platform.Security at startup. One privilege only, same
/// "immediate-effect, single-role-gated, no Maintainer/Approver split" shape as
/// <c>Modules.MasterData.Application.LookupSecurity</c> — real user administration in SAP/Dynamics is
/// gated by a security role, not a two-person approval workflow.
/// </summary>
public static class IdentitySecurity
{
    public const string AdministerPrivilegeKey = "Identity.User.Administer";

    public const string AdministratorDutyKey = "Identity.User.Administrator";

    public const string AdministratorRoleKey = "Identity.User.Administrator";

    public static readonly Duty AdministratorDuty = new(
        AdministratorDutyKey,
        "Create, deactivate, and manage role assignments for users",
        new[] { PrivilegeGrant.Unconditional(AdministerPrivilegeKey) });

    public static readonly Role AdministratorRole = new(
        AdministratorRoleKey, "User administrator", new[] { AdministratorDutyKey });
}
