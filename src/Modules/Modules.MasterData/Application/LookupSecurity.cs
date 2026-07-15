using Platform.Security;

namespace Modules.MasterData.Application;

/// <summary>
/// The Privilege/Duty/Role Lookup Data registers into Platform.Security at startup. One privilege only —
/// unlike every Business Object module (Maintainer + separate Approver, SoD-conflicted), because lookup
/// data has no approval lifecycle (see <see cref="Modules.MasterData.Domain.LookupType"/>'s doc comment):
/// real SAP domain-value maintenance and D365's Option Set editor are both gated by a single authorization
/// role, not a two-person maintain/approve split.
/// </summary>
public static class LookupSecurity
{
    public const string AdministerPrivilegeKey = "MasterData.Lookup.Administer";

    public const string AdministratorDutyKey = "MasterData.Lookup.Administrator";

    public const string AdministratorRoleKey = "MasterData.Lookup.Administrator";

    public static readonly Duty AdministratorDuty = new(
        AdministratorDutyKey,
        "Add, edit, deactivate, and delete admin-configurable lookup/picklist data (Countries, Business Role Types, Address Types, Units of Measure, Trades, and any future custom lookup type)",
        new[] { PrivilegeGrant.Unconditional(AdministerPrivilegeKey) });

    public static readonly Role AdministratorRole = new(
        AdministratorRoleKey, "Lookup Data administrator", new[] { AdministratorDutyKey });
}
