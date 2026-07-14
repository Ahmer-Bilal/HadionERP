using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Procurement.Application;

/// <summary>Same module-owned Security configuration pattern as <see cref="PurchaseRequisitionSecurity"/> —
/// a distinct Maintainer/Approver pair, since raising an RFQ and closing one out are a different real-world
/// authority than raising/approving a Purchase Requisition.</summary>
public static class RequestForQuotationSecurity
{
    public const string MaintainPrivilegeKey = "Procurement.RequestForQuotation.Maintain";
    public const string ApprovePrivilegeKey = "Procurement.RequestForQuotation.Approve";

    public const string MaintainerDutyKey = "Procurement.RequestForQuotation.Maintainer";
    public const string ApproverDutyKey = "Procurement.RequestForQuotation.Approver";

    public const string MaintainerRoleKey = "Procurement.RequestForQuotation.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Requests for Quotation (create, invite vendors, record quotes, submit)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve or reject a Request for Quotation",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Request for Quotation maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        RequestForQuotationWorkflow.ApproverRoleKey, "Request for Quotation approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both run and approve a Request for Quotation " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2).");
}
