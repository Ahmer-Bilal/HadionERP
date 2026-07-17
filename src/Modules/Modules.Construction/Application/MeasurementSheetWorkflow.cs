using Platform.Workflow;

namespace Modules.Construction.Application;

/// <summary>Same module-owned Workflow configuration pattern as every other module's first-cut BO — one
/// Any-quorum step. Named "Certify" rather than "Approve" to match the real-world action
/// (construction-commercial-processes-spec.md §2), even though it maps onto the platform's generic Approve
/// transition underneath.</summary>
public static class MeasurementSheetWorkflow
{
    public const string BusinessObjectType = "MeasurementSheet";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Construction.CertifyMeasurementSheet";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "MeasurementSheet.Certification.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Certify", RequiredRoleKey: ApproverRoleKey)
        });
}
