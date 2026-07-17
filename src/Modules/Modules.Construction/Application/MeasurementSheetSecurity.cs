using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Security configuration pattern as every other Construction BO — a
/// Maintainer/Approver pair, one Any-quorum SoD conflict rule. "Approver" here is the Client's Engineer
/// certifying measured quantities (construction-commercial-processes-spec.md §2), not a generic
/// document approval.</summary>
public static class MeasurementSheetSecurity
{
    public const string MaintainPrivilegeKey = "Construction.MeasurementSheet.Maintain";
    public const string ApprovePrivilegeKey = "Construction.MeasurementSheet.Approve";

    public const string MaintainerDutyKey = "Construction.MeasurementSheet.Maintainer";
    public const string ApproverDutyKey = "Construction.MeasurementSheet.Approver";

    public const string MaintainerRoleKey = "Construction.MeasurementSheet.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Measurement Sheets (record site quantities measured against a Contract or Subcontract's lines, submit for certification)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Certify (or reject) a Measurement Sheet's measured quantities",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Measurement Sheet maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        MeasurementSheetWorkflow.ApproverRoleKey, "Measurement Sheet approver (Engineer)", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both record measured quantities and certify them " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2) — mirrors the real-world " +
        "contractor-vs-Engineer split.");
}
