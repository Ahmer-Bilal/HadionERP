using Platform.Workflow;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Workflow configuration pattern as every other module's first-cut BO — one
/// Any-quorum step. A financial/commercial decision (release the withheld cash), not a physical-progress
/// certification, so the approver role is generic "Approver," the same convention <c>ContractWorkflow</c>
/// uses, not IPC's Engineer-flavored "Certify."</summary>
public static class RetentionReleaseWorkflow
{
    public const string BusinessObjectType = "RetentionRelease";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Construction.ApproveRetentionRelease";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "RetentionRelease.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
