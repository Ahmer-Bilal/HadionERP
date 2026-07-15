using Platform.Workflow;

namespace Modules.ProjectManagement.Application;

/// <summary>Same module-owned Workflow configuration pattern as every other module's first-cut BO — one
/// Any-quorum step. Approving a Project is its "release" (SAP PS's REL status) — the point other modules
/// should treat its WBS elements as postable, once a consumer actually exists.</summary>
public static class ProjectWorkflow
{
    public const string BusinessObjectType = "Project";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "ProjectManagement.ApproveProject";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "Project.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
