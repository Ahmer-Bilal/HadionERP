using Platform.Workflow;

namespace Modules.Procurement.Application;

/// <summary>
/// The approval matrix Vendor Prequalification submission is checked against — registered into
/// <see cref="IWorkflowDefinitionCatalog"/> at startup, same module-owned configuration pattern as every
/// prior BO's Workflow class. The FIRST real multi-step workflow in this codebase: five sequential,
/// Any-quorum, unconditioned steps (Commercial → Legal → Technical → HSE → Quality), per
/// ROADMAP.md's design — every one of these domains applies to every
/// <c>BusinessRoleType</c> a vendor can hold (only the review CRITERIA differ per role, not which steps
/// run), so no step needs a <see cref="WorkflowStepDefinition.Condition"/>. Confirmed feasible without any
/// new platform capability by reading <see cref="WorkflowEngine.Start"/>/<see cref="AttributeConstraints"/>
/// before building this — the roadmap's own stated intent ("no new platform capability required").
/// </summary>
public static class VendorPrequalificationWorkflow
{
    public const string BusinessObjectType = "VendorPrequalification";
    public const string SubmitTransition = "Submit";

    public const string CommercialReviewerRoleKey = "Procurement.VendorPrequalification.ReviewCommercial";
    public const string LegalReviewerRoleKey = "Procurement.VendorPrequalification.ReviewLegal";
    public const string TechnicalReviewerRoleKey = "Procurement.VendorPrequalification.ReviewTechnical";
    public const string HseReviewerRoleKey = "Procurement.VendorPrequalification.ReviewHse";
    public const string QualityReviewerRoleKey = "Procurement.VendorPrequalification.ReviewQuality";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "VendorPrequalification.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Commercial", RequiredRoleKey: CommercialReviewerRoleKey),
            new WorkflowStepDefinition(StepId: "Legal", RequiredRoleKey: LegalReviewerRoleKey),
            new WorkflowStepDefinition(StepId: "Technical", RequiredRoleKey: TechnicalReviewerRoleKey),
            new WorkflowStepDefinition(StepId: "HSE", RequiredRoleKey: HseReviewerRoleKey),
            new WorkflowStepDefinition(StepId: "Quality", RequiredRoleKey: QualityReviewerRoleKey),
        });
}
