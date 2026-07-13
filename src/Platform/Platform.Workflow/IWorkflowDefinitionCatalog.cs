namespace Platform.Workflow;

/// <summary>
/// The registered set of approval-workflow definitions. In production this is populated from
/// configuration (docs/architecture/04-data-and-api.md #3) — a functional consultant maintains approval
/// matrices through an admin UI, they are not hard-coded per module.
/// </summary>
public interface IWorkflowDefinitionCatalog
{
    /// <summary>Resolves the definition attached to a Business Object type + triggering transition, or
    /// null if none is configured (meaning: no workflow required, the caller proceeds without one).</summary>
    WorkflowDefinition? Resolve(string businessObjectType, string triggerTransition);
}
