using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Security configuration pattern as every other module's first-cut BO — a
/// Maintainer/Approver pair, one Any-quorum SoD conflict rule.</summary>
public static class SubcontractSecurity
{
    public const string MaintainPrivilegeKey = "Construction.Subcontract.Maintain";
    public const string ApprovePrivilegeKey = "Construction.Subcontract.Approve";

    public const string MaintainerDutyKey = "Construction.Subcontract.Maintainer";
    public const string ApproverDutyKey = "Construction.Subcontract.Approver";

    public const string MaintainerRoleKey = "Construction.Subcontract.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Subcontracts (build the scope-of-work lines mapped onto Project WBS elements, submit for approval, record back-charges)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Subcontract",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Subcontract maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        SubcontractWorkflow.ApproverRoleKey, "Subcontract approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both build and approve a Subcontract " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
