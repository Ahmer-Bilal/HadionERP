using Platform.Workflow;

namespace Modules.Finance.Application;

/// <summary>
/// The approval matrix Journal Entry creation is checked against — registered into
/// <see cref="IWorkflowDefinitionCatalog"/> at startup, same module-owned configuration pattern as every
/// Modules.MasterData slice. One step, Any-quorum for Phase 1 — a dual-approval matrix for entries above a
/// threshold is a real future need (SAP-style), deferred until Finance has more than one approval path to
/// choose between.
/// </summary>
public static class JournalEntryWorkflow
{
    public const string BusinessObjectType = "JournalEntry";
    public const string SubmitTransition = "Submit";
    public const string ApproverRoleKey = "Finance.ApproveJournalEntry";

    public static readonly WorkflowDefinition SubmitApprovalDefinition = new(
        DefinitionKey: "JournalEntry.Approval.v1",
        BusinessObjectType: BusinessObjectType,
        TriggerTransition: SubmitTransition,
        Steps: new[]
        {
            new WorkflowStepDefinition(StepId: "Approve", RequiredRoleKey: ApproverRoleKey)
        });
}
