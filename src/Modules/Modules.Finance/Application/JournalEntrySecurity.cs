using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Finance.Application;

/// <summary>
/// The Privileges/Duties/Roles/SoD conflict rule Journal Entry registers into Platform.Security at
/// startup — same module-owned security configuration pattern as every Modules.MasterData slice. Posting a
/// GL entry is the single highest-stakes action in the whole platform so far — the same person should
/// never both create/maintain and approve/post one.
/// </summary>
public static class JournalEntrySecurity
{
    public const string MaintainPrivilegeKey = "Finance.JournalEntry.Maintain";
    public const string ApprovePrivilegeKey = "Finance.JournalEntry.Approve";

    public const string MaintainerDutyKey = "Finance.JournalEntry.Maintainer";
    public const string ApproverDutyKey = "Finance.JournalEntry.Approver";

    public const string MaintainerRoleKey = "Finance.JournalEntry.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Journal Entries (create, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve, post, or reverse a Journal Entry",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Journal Entry maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        JournalEntryWorkflow.ApproverRoleKey, "Journal Entry approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve/post a Journal Entry " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
