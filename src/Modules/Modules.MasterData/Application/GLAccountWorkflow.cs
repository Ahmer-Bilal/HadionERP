using Platform.Workflow;

namespace Modules.MasterData.Application;

/// <summary>
/// The approval matrix G/L Account creation is checked against — registered into
/// <see cref="IWorkflowDefinitionCatalog"/> at startup, the same module-owned configuration pattern as
/// <see cref="BusinessPartnerWorkflow"/>. One step, Any-quorum: any single holder of the Approver role can
/// approve a new account. The chart of accounts is a stable, small set; a more elaborate matrix (e.g.
/// dual approval for balance-sheet accounts) would grow a condition-gated second step through configuration.
/// </summary>
public static class GLAccountWorkflow
{
    public const string BusinessObjectType = "GLAccount";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "MasterData.ApproveGLAccount";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "GLAccount.Onboarding.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
