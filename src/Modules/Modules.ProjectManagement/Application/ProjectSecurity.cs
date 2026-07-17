using Platform.Security;
using Platform.Security.Sod;

namespace Modules.ProjectManagement.Application;

/// <summary>Same module-owned Security configuration pattern as every other module's first-cut BO — a
/// Maintainer/Approver pair, one Any-quorum SoD conflict rule.</summary>
public static class ProjectSecurity
{
    public const string MaintainPrivilegeKey = "ProjectManagement.Project.Maintain";
    public const string ApprovePrivilegeKey = "ProjectManagement.Project.Approve";

    public const string MaintainerDutyKey = "ProjectManagement.Project.Maintainer";
    public const string ApproverDutyKey = "ProjectManagement.Project.Approver";

    public const string MaintainerRoleKey = "ProjectManagement.Project.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Projects (define the project, build its WBS structure, submit for release)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve (release) or reject a Project",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Project maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        ProjectWorkflow.ApproverRoleKey, "Project approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both define and release a Project " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
