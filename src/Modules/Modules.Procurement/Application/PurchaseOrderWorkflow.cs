using Platform.Workflow;

namespace Modules.Procurement.Application;

/// <summary>Same module-owned Workflow configuration pattern as <see cref="RequestForQuotationWorkflow"/> —
/// one Any-quorum step. The Finance budget check (<c>PurchaseOrderService.SubmitAsync</c>) runs before this
/// workflow starts, matching docs/architecture/01-architecture-foundation.md §3.2's own example ("Procurement
/// asks Finance's IBudgetCheckService before releasing a PO") — a PO that fails budget never reaches an
/// approver at all.</summary>
public static class PurchaseOrderWorkflow
{
    public const string BusinessObjectType = "PurchaseOrder";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Procurement.ApprovePurchaseOrder";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "PurchaseOrder.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
