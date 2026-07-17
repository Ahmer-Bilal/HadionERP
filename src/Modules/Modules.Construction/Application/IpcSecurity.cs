using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Security configuration pattern as every other Construction BO — a
/// Maintainer/Approver pair, one Any-quorum SoD conflict rule. "Approver" here is the Client's Engineer
/// certifying the IPC (construction-commercial-processes-spec.md §3), not a generic document approval.</summary>
public static class IpcSecurity
{
    public const string MaintainPrivilegeKey = "Construction.Ipc.Maintain";
    public const string ApprovePrivilegeKey = "Construction.Ipc.Approve";

    public const string MaintainerDutyKey = "Construction.Ipc.Maintainer";
    public const string ApproverDutyKey = "Construction.Ipc.Approver";

    public const string MaintainerRoleKey = "Construction.Ipc.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Prepare Interim Payment Certificates from a certified Measurement Sheet and submit them for certification",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Certify (or reject) an Interim Payment Certificate",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "IPC maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        IpcWorkflow.ApproverRoleKey, "IPC approver (Engineer)", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both prepare an IPC and certify it " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2) — mirrors the real-world " +
        "contractor-vs-Engineer split.");
}
