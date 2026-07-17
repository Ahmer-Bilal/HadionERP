using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Security configuration pattern as every other Construction BO — a
/// Maintainer/Approver pair, one Any-quorum SoD conflict rule.</summary>
public static class VariationOrderSecurity
{
    public const string MaintainPrivilegeKey = "Construction.VariationOrder.Maintain";
    public const string ApprovePrivilegeKey = "Construction.VariationOrder.Approve";

    public const string MaintainerDutyKey = "Construction.VariationOrder.Maintainer";
    public const string ApproverDutyKey = "Construction.VariationOrder.Approver";

    public const string MaintainerRoleKey = "Construction.VariationOrder.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Variation Orders (propose scope/quantity changes against a Contract or Subcontract, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve (or reject) a Variation Order — the decision that writes its scope/quantity change through to the underlying Contract or Subcontract",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Variation Order maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        VariationOrderWorkflow.ApproverRoleKey, "Variation Order approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both propose a Variation Order and approve it " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
