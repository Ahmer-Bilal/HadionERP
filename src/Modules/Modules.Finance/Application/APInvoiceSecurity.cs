using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Security configuration pattern as <see cref="JournalEntrySecurity"/> — a
/// deliberately separate Duty/Role pair from Journal Entry's, since approving an AP invoice (a payment
/// obligation to a vendor) and approving a GL journal entry are different real-world authorities, even
/// though this module currently grants both to the same demo actor for lack of real role assignment.</summary>
public static class APInvoiceSecurity
{
    public const string MaintainPrivilegeKey = "Finance.APInvoice.Maintain";
    public const string ApprovePrivilegeKey = "Finance.APInvoice.Approve";

    public const string MaintainerDutyKey = "Finance.APInvoice.Maintainer";
    public const string ApproverDutyKey = "Finance.APInvoice.Approver";

    public const string MaintainerRoleKey = "Finance.APInvoice.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain AP Invoices (create, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve, post, or reverse an AP Invoice",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "AP Invoice maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        APInvoiceWorkflow.ApproverRoleKey, "AP Invoice approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve/post an AP Invoice " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2) — the textbook " +
        "\"Create Vendor Invoice vs. Approve Vendor Payment\" example.");
}
