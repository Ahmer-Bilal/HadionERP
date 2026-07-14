using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Procurement.Application;

/// <summary>Same module-owned Security configuration pattern as every prior BO's Security class — a
/// Maintainer/Approver pair distinct from Vendor Prequalification's five reviewer duties, since raising a
/// purchase requisition and approving one are a different real-world authority than vendor qualification
/// review.</summary>
public static class PurchaseRequisitionSecurity
{
    public const string MaintainPrivilegeKey = "Procurement.PurchaseRequisition.Maintain";
    public const string ApprovePrivilegeKey = "Procurement.PurchaseRequisition.Approve";

    public const string MaintainerDutyKey = "Procurement.PurchaseRequisition.Maintainer";
    public const string ApproverDutyKey = "Procurement.PurchaseRequisition.Approver";

    public const string MaintainerRoleKey = "Procurement.PurchaseRequisition.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Purchase Requisitions (create, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Purchase Requisition",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Purchase Requisition maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        PurchaseRequisitionWorkflow.ApproverRoleKey, "Purchase Requisition approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both raise and approve a Purchase Requisition " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2).");
}
