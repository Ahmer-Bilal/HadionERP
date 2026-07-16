using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Security configuration pattern as every other module's first-cut BO — a
/// Maintainer/Approver pair, one Any-quorum SoD conflict rule.</summary>
public static class ContractSecurity
{
    public const string MaintainPrivilegeKey = "Construction.Contract.Maintain";
    public const string ApprovePrivilegeKey = "Construction.Contract.Approve";

    public const string MaintainerDutyKey = "Construction.Contract.Maintainer";
    public const string ApproverDutyKey = "Construction.Contract.Approver";

    public const string MaintainerRoleKey = "Construction.Contract.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Contracts (build the BOQ mapped onto Project WBS elements, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Contract",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Contract maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        ContractWorkflow.ApproverRoleKey, "Contract approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both build and approve a Contract " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2).");
}
