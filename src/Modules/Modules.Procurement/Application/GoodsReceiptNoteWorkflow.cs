using Platform.Workflow;

namespace Modules.Procurement.Application;

/// <summary>Same module-owned Workflow configuration pattern as <see cref="PurchaseOrderWorkflow"/> — one
/// Any-quorum step.</summary>
public static class GoodsReceiptNoteWorkflow
{
    public const string BusinessObjectType = "GoodsReceiptNote";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Procurement.ApproveGoodsReceiptNote";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "GoodsReceiptNote.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
