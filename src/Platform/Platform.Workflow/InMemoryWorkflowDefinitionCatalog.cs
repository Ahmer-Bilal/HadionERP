namespace Platform.Workflow;

/// <summary>Reference implementation of <see cref="IWorkflowDefinitionCatalog"/> — same "swap for a
/// database-backed implementation later behind the same interface" pattern as everywhere else in the
/// platform kernel (e.g. Platform.Core.NumberRanges.InMemoryNumberRangeService).</summary>
public sealed class InMemoryWorkflowDefinitionCatalog : IWorkflowDefinitionCatalog
{
    private readonly Dictionary<(string BusinessObjectType, string TriggerTransition), WorkflowDefinition> _definitions;

    public InMemoryWorkflowDefinitionCatalog(IEnumerable<WorkflowDefinition> definitions)
    {
        _definitions = definitions.ToDictionary(d => (d.BusinessObjectType, d.TriggerTransition));
    }

    public WorkflowDefinition? Resolve(string businessObjectType, string triggerTransition) =>
        _definitions.GetValueOrDefault((businessObjectType, triggerTransition));
}
