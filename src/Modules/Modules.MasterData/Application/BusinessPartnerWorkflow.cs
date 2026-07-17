using Platform.Workflow;

namespace Modules.MasterData.Application;

/// <summary>
/// The approval matrix Business Partner onboarding is checked against — registered into
/// <see cref="IWorkflowDefinitionCatalog"/> at startup (see <c>Gateway.Api/Program.cs</c>), the same way
/// <see cref="BusinessPartnerService.NumberRangeKey"/> is registered into the number-range catalog. This is
/// module-owned configuration data (docs/architecture/04-platform-services.md #4: "attached... via
/// configuration, not code"), not workflow engine logic — the engine itself has no idea Business Partner
/// exists.
///
/// One step, Any-quorum: any single holder of <see cref="ApproverRoleKey"/> can approve. This is
/// intentionally simple for the first real workflow wired into a module — a real approval matrix (a
/// second approver above a risk threshold, a different approver for KSA-resident vs. foreign vendors) is
/// exactly what this same definition would grow a `Condition`-gated second step for later, entirely
/// through configuration, without touching <see cref="BusinessPartnerService"/>.
/// </summary>
public static class BusinessPartnerWorkflow
{
    public const string BusinessObjectType = "BusinessPartner";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "MasterData.ApproveBusinessPartner";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "BusinessPartner.Onboarding.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
