using Platform.Workflow;

namespace Modules.MasterData.Application;

/// <summary>
/// The approval matrix Cost Center creation is checked against — registered into
/// <see cref="IWorkflowDefinitionCatalog"/> at startup, same module-owned configuration pattern as
/// <see cref="GLAccountWorkflow"/>/<see cref="ItemWorkflow"/>. One step, Any-quorum: any single holder of
/// the Approver role can approve a new cost center.
/// </summary>
public static class CostCenterWorkflow
{
    public const string BusinessObjectType = "CostCenter";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "MasterData.ApproveCostCenter";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "CostCenter.Onboarding.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
