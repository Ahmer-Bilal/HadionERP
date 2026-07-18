using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Security configuration pattern as every other Construction BO — a
/// Maintainer/Approver pair, one Any-quorum SoD conflict rule.</summary>
public static class RetentionReleaseSecurity
{
    public const string MaintainPrivilegeKey = "Construction.RetentionRelease.Maintain";
    public const string ApprovePrivilegeKey = "Construction.RetentionRelease.Approve";

    public const string MaintainerDutyKey = "Construction.RetentionRelease.Maintainer";
    public const string ApproverDutyKey = "Construction.RetentionRelease.Approver";

    public const string MaintainerRoleKey = "Construction.RetentionRelease.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Prepare a Retention Release against a Contract or Subcontract's withheld retention balance and submit it for approval",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Retention Release",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Retention release maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        RetentionReleaseWorkflow.ApproverRoleKey, "Retention release approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both prepare a Retention Release and approve it " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
