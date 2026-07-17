using Platform.Security;
using Platform.Security.Sod;

namespace Modules.MasterData.Application;

/// <summary>
/// The Privileges/Duties/Roles/SoD conflict rule Business Partner registers into `Platform.Security` at
/// startup (see <c>Gateway.Api/Program.cs</c>) — module-owned security configuration, the same
/// "attached via configuration, not code" pattern <see cref="BusinessPartnerWorkflow"/> already uses for
/// its approval matrix. `Platform.Security` itself has no idea Business Partner exists.
///
/// Two Duties, deliberately split rather than one "can do everything" Duty: Maintainer (create, add
/// address/contact, submit) and Approver (approve/reject) — the classic Segregation of Duties split this
/// module's own domain comments already reference ("new-partner onboarding is a real fraud/compliance
/// control point"), and literally the worked example in <see cref="Duty"/>'s own doc comment ("Create
/// Vendor" vs. "Approve Vendor Payment"). <see cref="MaintainerApproverConflict"/> registers that as a
/// real, checkable SoD rule — see <c>docs/module/master-data.md</c> for what enforcing it against a
/// real role *assignment* would still need (an admin UI to assign roles doesn't exist yet, so there's
/// nowhere to check the rule against at assignment time; the rule and the engine that checks it are both
/// real and tested today).
///
/// <see cref="ApproverRole"/> reuses <see cref="BusinessPartnerWorkflow.ApproverRoleKey"/> as its Role
/// key on purpose — the same Role means "can approve Business Partners" to both `Platform.Workflow`'s
/// step-eligibility check and `Platform.Security`'s privilege-grant resolution, so a security
/// administrator manages one Role, not two.
/// </summary>
public static class BusinessPartnerSecurity
{
    public const string MaintainPrivilegeKey = "MasterData.BusinessPartner.Maintain";
    public const string ApprovePrivilegeKey = "MasterData.BusinessPartner.Approve";

    public const string MaintainerDutyKey = "MasterData.BusinessPartner.Maintainer";
    public const string ApproverDutyKey = "MasterData.BusinessPartner.Approver";

    public const string MaintainerRoleKey = "MasterData.BusinessPartner.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Business Partners (create, addresses, contacts, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Business Partner's onboarding",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Business Partner maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        BusinessPartnerWorkflow.ApproverRoleKey, "Business Partner approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve a Business Partner " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
