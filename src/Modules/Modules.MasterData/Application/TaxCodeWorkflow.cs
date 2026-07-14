using Platform.Workflow;

namespace Modules.MasterData.Application;

/// <summary>
/// The approval matrix Tax Code creation is checked against — registered into
/// <see cref="IWorkflowDefinitionCatalog"/> at startup, same module-owned configuration pattern as every
/// other Phase 1 master-data entity. One step, Any-quorum.
/// </summary>
public static class TaxCodeWorkflow
{
    public const string BusinessObjectType = "TaxCode";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "MasterData.ApproveTaxCode";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "TaxCode.Onboarding.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
