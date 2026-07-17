using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Security configuration pattern as <see cref="PaymentSecurity"/>.</summary>
public static class CustomerReceiptSecurity
{
    public const string MaintainPrivilegeKey = "Finance.CustomerReceipt.Maintain";
    public const string ApprovePrivilegeKey = "Finance.CustomerReceipt.Approve";

    public const string MaintainerDutyKey = "Finance.CustomerReceipt.Maintainer";
    public const string ApproverDutyKey = "Finance.CustomerReceipt.Approver";

    public const string MaintainerRoleKey = "Finance.CustomerReceipt.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Customer Receipts (create, add/remove allocations, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve, post, or reverse a Customer Receipt",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Customer Receipt maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        CustomerReceiptWorkflow.ApproverRoleKey, "Customer Receipt approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve/post a Customer Receipt " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
