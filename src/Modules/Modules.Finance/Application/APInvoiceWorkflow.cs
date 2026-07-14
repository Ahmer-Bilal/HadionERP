using Platform.Workflow;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Workflow configuration pattern as <see cref="JournalEntryWorkflow"/>. One
/// step, Any-quorum for Phase 1.</summary>
public static class APInvoiceWorkflow
{
    public const string BusinessObjectType = "APInvoice";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Finance.ApproveAPInvoice";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "APInvoice.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
