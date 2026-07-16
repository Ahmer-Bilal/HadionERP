using Platform.Workflow;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Workflow configuration pattern as every other module's first-cut BO — one
/// Any-quorum step.</summary>
public static class SubcontractWorkflow
{
    public const string BusinessObjectType = "Subcontract";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Construction.ApproveSubcontract";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "Subcontract.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
