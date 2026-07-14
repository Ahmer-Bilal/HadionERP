using Platform.Workflow;

namespace Modules.MasterData.Application;

/// <summary>
/// The approval matrix Item creation is checked against — registered into
/// <see cref="IWorkflowDefinitionCatalog"/> at startup, same module-owned configuration pattern as
/// <see cref="BusinessPartnerWorkflow"/>/<see cref="GLAccountWorkflow"/>. One step, Any-quorum: any single
/// holder of the Approver role can approve a new item.
/// </summary>
public static class ItemWorkflow
{
    public const string BusinessObjectType = "Item";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "MasterData.ApproveItem";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "Item.Onboarding.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
