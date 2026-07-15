using Platform.Workflow;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Workflow configuration pattern as <see cref="APInvoiceWorkflow"/>. One step,
/// Any-quorum, same first-cut shape every module uses.</summary>
public static class BankAccountWorkflow
{
    public const string BusinessObjectType = "BankAccount";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Finance.ApproveBankAccount";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "BankAccount.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
