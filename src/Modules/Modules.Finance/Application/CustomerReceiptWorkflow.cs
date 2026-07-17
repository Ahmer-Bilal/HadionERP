using Platform.Workflow;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Workflow configuration pattern as <see cref="PaymentWorkflow"/>.</summary>
public static class CustomerReceiptWorkflow
{
    public const string BusinessObjectType = "CustomerReceipt";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Finance.ApproveCustomerReceipt";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "CustomerReceipt.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
