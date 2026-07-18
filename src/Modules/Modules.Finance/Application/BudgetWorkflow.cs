using Platform.Workflow;

namespace Modules.Finance.Application;

/// <summary>
/// The approval matrix Budget creation is checked against — registered into
/// <see cref="IWorkflowDefinitionCatalog"/> at startup, same module-owned configuration pattern as
/// <see cref="JournalEntryWorkflow"/>. One step, Any-quorum for this first slice — same deferred
/// "dual-approval above a threshold" future need noted on Journal Entry's own workflow.
/// </summary>
public static class BudgetWorkflow
{
    public const string BusinessObjectType = "Budget";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Finance.ApproveBudget";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "Budget.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
