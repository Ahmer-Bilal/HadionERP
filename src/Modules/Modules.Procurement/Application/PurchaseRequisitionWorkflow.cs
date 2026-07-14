using Platform.Workflow;

namespace Modules.Procurement.Application;

/// <summary>Same module-owned Workflow configuration pattern as <see cref="VendorPrequalificationWorkflow"/>
/// (and every prior module's own Workflow class). One step, Any-quorum for Phase 2's first cut — a real
/// approval matrix conditioned on estimated amount is a natural later refinement (the roadmap's own named
/// example, "a second approver step for a PO over a threshold"), deferred until this module has more than
/// one approval path to choose between.</summary>
public static class PurchaseRequisitionWorkflow
{
    public const string BusinessObjectType = "PurchaseRequisition";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Procurement.ApprovePurchaseRequisition";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "PurchaseRequisition.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
