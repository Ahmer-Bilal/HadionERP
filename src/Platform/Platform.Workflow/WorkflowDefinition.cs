namespace Platform.Workflow;

/// <summary>
/// A configured approval matrix — attached to a Business Object type + the transition that triggers it
/// (e.g. "PurchaseOrder.Submit triggers workflow PO_Approval_v3", docs/architecture/03-platform-services.md
/// #4). This is configuration data, not code — a functional consultant maintains these through the
/// eventual admin UI (docs/architecture/04-data-and-api.md #3), not a release.
/// </summary>
public sealed record WorkflowDefinition(
    string DefinitionKey,
    string BusinessObjectType,
    string TriggerTransition,
    IReadOnlyList<WorkflowStepDefinition> Steps);
