using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Procurement.Application;

/// <summary>Same module-owned Security configuration pattern as <see cref="PurchaseOrderSecurity"/> — a
/// distinct Maintainer/Approver pair, since confirming a delivery is a different real-world authority
/// (typically the site/warehouse team) than raising or approving a PO.</summary>
public static class GoodsReceiptNoteSecurity
{
    public const string MaintainPrivilegeKey = "Procurement.GoodsReceiptNote.Maintain";
    public const string ApprovePrivilegeKey = "Procurement.GoodsReceiptNote.Approve";

    public const string MaintainerDutyKey = "Procurement.GoodsReceiptNote.Maintainer";
    public const string ApproverDutyKey = "Procurement.GoodsReceiptNote.Approver";

    public const string MaintainerRoleKey = "Procurement.GoodsReceiptNote.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Record and maintain Goods Receipt Notes (create, submit)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Goods Receipt Note",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Goods Receipt Note maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        GoodsReceiptNoteWorkflow.ApproverRoleKey, "Goods Receipt Note approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both record and approve a Goods Receipt Note " +
        "(Segregation of Duties, docs/architecture/04-platform-services.md #2.2).");
}
