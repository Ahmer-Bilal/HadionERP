using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Security configuration pattern as <see cref="APInvoiceSecurity"/> — a
/// deliberately separate Duty/Role pair from AP Invoice's, since raising a customer bill and approving a
/// vendor payment obligation are different real-world authorities.</summary>
public static class ARInvoiceSecurity
{
    public const string MaintainPrivilegeKey = "Finance.ARInvoice.Maintain";
    public const string ApprovePrivilegeKey = "Finance.ARInvoice.Approve";

    public const string MaintainerDutyKey = "Finance.ARInvoice.Maintainer";
    public const string ApproverDutyKey = "Finance.ARInvoice.Approver";

    public const string MaintainerRoleKey = "Finance.ARInvoice.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain AR Invoices (create, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve, post, or reverse an AR Invoice",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "AR Invoice maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        ARInvoiceWorkflow.ApproverRoleKey, "AR Invoice approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve/post an AR Invoice " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
