using Platform.Workflow;

namespace Modules.Procurement.Application;

/// <summary>Same module-owned Workflow configuration pattern as <see cref="PurchaseRequisitionWorkflow"/> —
/// one Any-quorum step. Approving an RFQ means "the quote-collection process is closed, ready for a PO to
/// reference," not a financial approval.</summary>
public static class RequestForQuotationWorkflow
{
    public const string BusinessObjectType = "RequestForQuotation";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Procurement.ApproveRequestForQuotation";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "RequestForQuotation.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
