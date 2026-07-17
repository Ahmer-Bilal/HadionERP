using Platform.Workflow;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Workflow configuration pattern as <see cref="APInvoiceWorkflow"/>. One step,
/// Any-quorum.</summary>
public static class ARInvoiceWorkflow
{
    public const string BusinessObjectType = "ARInvoice";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Finance.ApproveARInvoice";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "ARInvoice.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
