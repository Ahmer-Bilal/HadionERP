using Platform.Workflow;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Workflow configuration pattern as every other module's first-cut BO — one
/// Any-quorum step. Named "Certify" rather than "Approve" to match the real-world action
/// (construction-commercial-processes-spec.md §3: certification is what legally obligates payment), even
/// though it maps onto the platform's generic Approve transition underneath.</summary>
public static class IpcWorkflow
{
    public const string BusinessObjectType = "Ipc";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Construction.CertifyIpc";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "Ipc.Certification.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Certify", RequiredRoleKey: ApproverRoleKey)
        });
}
