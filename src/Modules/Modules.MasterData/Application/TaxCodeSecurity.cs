using Platform.Security;
using Platform.Security.Sod;

namespace Modules.MasterData.Application;

/// <summary>
/// The Privileges/Duties/Roles/SoD conflict rule Tax Code registers into Platform.Security at startup —
/// same module-owned security configuration pattern as every other Phase 1 master-data entity. A wrong
/// VAT rate/type on a tax code affects every AP/AR document referencing it afterward, the same control-
/// point reasoning as the rest of Master Data.
/// </summary>
public static class TaxCodeSecurity
{
    public const string MaintainPrivilegeKey = "MasterData.TaxCode.Maintain";
    public const string ApprovePrivilegeKey = "MasterData.TaxCode.Approve";

    public const string MaintainerDutyKey = "MasterData.TaxCode.Maintainer";
    public const string ApproverDutyKey = "MasterData.TaxCode.Approver";

    public const string MaintainerRoleKey = "MasterData.TaxCode.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Tax Codes (create, update, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Tax Code",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Tax Code maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        TaxCodeWorkflow.ApproverRoleKey, "Tax Code approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve a Tax Code " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
