using Platform.Core;
using Platform.Security;

namespace Platform.Workflow;

public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowDefinitionCatalog _catalog;
    private readonly IWorkflowEligibilityService _eligibilityService;

    public WorkflowEngine(IWorkflowDefinitionCatalog catalog, IWorkflowEligibilityService eligibilityService)
    {
        _catalog = catalog;
        _eligibilityService = eligibilityService;
    }

    public WorkflowInstance? Start(
        string businessObjectType,
        string triggerTransition,
        Guid businessObjectId,
        IReadOnlyDictionary<string, string>? resourceContext = null,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? requiredApproversByStep = null)
    {
        var definition = _catalog.Resolve(businessObjectType, triggerTransition);
        if (definition is null)
        {
            return null;
        }

        var applicableSteps = definition.Steps
            .Where(step => AttributeConstraints.Satisfies(step.Condition, resourceContext))
            .ToList();

        return new WorkflowInstance(definition.DefinitionKey, businessObjectId, businessObjectType, applicableSteps, requiredApproversByStep);
    }

    public void Decide(WorkflowInstance instance, SecurityPrincipal actor, WorkflowDecision decision, string? comment = null, DateOnly? onDate = null)
    {
        var step = instance.CurrentStep
            ?? throw new InvalidOperationException($"Workflow instance {instance.Id} has no current step to decide (status: {instance.Status}).");

        var effectiveDate = onDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (!_eligibilityService.CanAct(actor, step, effectiveDate))
        {
            throw new UnauthorizedAccessException(
                $"'{actor.UserId}' is not eligible to act on step '{step.StepId}' (requires role '{step.RequiredRoleKey}').");
        }

        instance.Decide(actor.UserId, decision, comment);
    }

    public void Cancel(WorkflowInstance instance, SecurityPrincipal actor, string reason) => instance.Cancel(actor.UserId, reason);
}
