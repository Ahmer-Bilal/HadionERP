using Platform.Security;

namespace Modules.Finance.Application;

/// <summary>
/// The Privilege/Duty/Role Fiscal Year/Period registers into Platform.Security at startup. One privilege
/// only — same reasoning as <see cref="Modules.MasterData.Application.LookupSecurity"/>: opening a year or
/// closing/reopening one of its periods has no approval lifecycle (see <see cref="Domain.FiscalYear"/>'s
/// own doc comment), it's a single Finance Controller action gated by a role, not a two-person split.
/// </summary>
public static class FiscalYearSecurity
{
    public const string AdministerPrivilegeKey = "Finance.FiscalYear.Administer";

    public const string AdministratorDutyKey = "Finance.FiscalYear.Administrator";

    public const string AdministratorRoleKey = "Finance.FiscalYear.Administrator";

    public static readonly Duty AdministratorDuty = new(
        AdministratorDutyKey,
        "Open a Fiscal Year and close/reopen its Periods",
        new[] { PrivilegeGrant.Unconditional(AdministerPrivilegeKey) });

    public static readonly Role AdministratorRole = new(
        AdministratorRoleKey, "Fiscal Year administrator", new[] { AdministratorDutyKey });
}
