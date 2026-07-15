using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Procurement.Application;

/// <summary>Same module-owned Security configuration pattern as <see cref="RequestForQuotationSecurity"/> —
/// a distinct Maintainer/Approver pair, since raising a PO is a different real-world authority than raising
/// an RFQ or a PR.</summary>
public static class PurchaseOrderSecurity
{
    public const string MaintainPrivilegeKey = "Procurement.PurchaseOrder.Maintain";
    public const string ApprovePrivilegeKey = "Procurement.PurchaseOrder.Approve";

    public const string MaintainerDutyKey = "Procurement.PurchaseOrder.Maintainer";
    public const string ApproverDutyKey = "Procurement.PurchaseOrder.Approver";

    public const string MaintainerRoleKey = "Procurement.PurchaseOrder.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Purchase Orders (create from an RFQ-selected quote or direct, submit)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Purchase Order",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Purchase Order maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        PurchaseOrderWorkflow.ApproverRoleKey, "Purchase Order approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both raise and approve a Purchase Order " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2).");
}
