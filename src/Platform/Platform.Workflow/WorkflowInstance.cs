using Platform.Core.Events;
using Platform.Workflow.Events;

namespace Platform.Workflow;

/// <summary>
/// One run of a <see cref="WorkflowDefinition"/> against one Business Object. Owns its own lifecycle
/// (Running → Approved/Rejected/Cancelled) — a distinct lifecycle from the Business Object's own
/// Draft/Submitted/Posted lifecycle (Platform.Core.BusinessObjectStatus); the two are linked by
/// <see cref="WorkflowCompletedEvent"/>, not merged into one state machine.
///
/// Instances are created via <see cref="WorkflowEngine.Start"/>, not directly — construction resolves
/// which steps actually apply (condition-based routing) and auto-approves immediately if none do (e.g. a
/// PO under every threshold needs no approval at all).
/// </summary>
public sealed class WorkflowInstance
{
    private readonly List<WorkflowStepDecisionRecord> _history = new();
    private readonly Dictionary<string, HashSet<string>> _requiredApproversByStep;
    private readonly Dictionary<string, HashSet<string>> _approvedByStep = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; }
    public string DefinitionKey { get; }
    public Guid BusinessObjectId { get; }
    public string BusinessObjectType { get; }
    public IReadOnlyList<WorkflowStepDefinition> ApplicableSteps { get; }
    public WorkflowInstanceStatus Status { get; private set; }
    public int CurrentStepIndex { get; private set; }
    public DateTimeOffset? CurrentStepStartedAt { get; private set; }

    public IReadOnlyList<WorkflowStepDecisionRecord> History => _history.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public WorkflowStepDefinition? CurrentStep =>
        Status == WorkflowInstanceStatus.Running && CurrentStepIndex < ApplicableSteps.Count
            ? ApplicableSteps[CurrentStepIndex]
            : null;

    internal WorkflowInstance(
        string definitionKey,
        Guid businessObjectId,
        string businessObjectType,
        IReadOnlyList<WorkflowStepDefinition> applicableSteps,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? requiredApproversByStep)
    {
        Id = Guid.NewGuid();
        DefinitionKey = definitionKey;
        BusinessObjectId = businessObjectId;
        BusinessObjectType = businessObjectType;
        ApplicableSteps = applicableSteps;

        _requiredApproversByStep = (requiredApproversByStep ?? new Dictionary<string, IReadOnlyCollection<string>>())
            .ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value));

        if (ApplicableSteps.Count == 0)
        {
            // No configured step applies to this resource context (e.g. an amount under every
            // threshold) — there is nothing to approve, so the instance completes immediately.
            Status = WorkflowInstanceStatus.Approved;
            _domainEvents.Add(WorkflowCompletedEvent.Create(Id, BusinessObjectId, BusinessObjectType, Status, "No applicable approval step."));
        }
        else
        {
            Status = WorkflowInstanceStatus.Running;
            CurrentStepStartedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>Reserved for ORM materialization (see the persistence port's implementation) — never call
    /// from application code; use <see cref="WorkflowEngine.Start"/> to create a new instance. Every
    /// property/field below is set directly by the ORM via reflection after this runs (the same pattern
    /// <c>Platform.Core.BusinessObject</c>'s own parameterless constructor uses), including the
    /// otherwise-`readonly` dictionary fields — EF Core explicitly supports materializing into readonly
    /// backing fields (see EF Core's "read-only fields" backing-field docs).</summary>
    private WorkflowInstance()
    {
        DefinitionKey = null!;
        BusinessObjectType = null!;
        ApplicableSteps = null!;
        _requiredApproversByStep = null!;
    }

    /// <summary>Required approvers snapshotted for the current step (All-quorum only — empty for Any).</summary>
    public IReadOnlyCollection<string> RequiredApproversForCurrentStep =>
        CurrentStep is { } step && _requiredApproversByStep.TryGetValue(step.StepId, out var required)
            ? required
            : Array.Empty<string>();

    /// <summary>
    /// Records a decision against the current step. Callers (WorkflowEngine) are responsible for
    /// checking the actor is eligible to act on this step before calling this — this method enforces
    /// only the workflow's own structural rules (must be Running, must be a required approver for
    /// All-quorum steps), not "who is allowed to approve," which is an authorization concern.
    /// </summary>
    public void Decide(string actorUserId, WorkflowDecision decision, string? comment = null)
    {
        if (Status != WorkflowInstanceStatus.Running)
        {
            throw new InvalidOperationException($"Workflow instance {Id} is not running (status: {Status}).");
        }

        var step = CurrentStep!;

        if (step.Quorum == ApprovalQuorum.All
            && _requiredApproversByStep.TryGetValue(step.StepId, out var required)
            && required.Count > 0
            && !required.Contains(actorUserId))
        {
            throw new ArgumentException(
                $"'{actorUserId}' is not one of the required approvers for step '{step.StepId}'.", nameof(actorUserId));
        }

        _history.Add(new WorkflowStepDecisionRecord(step.StepId, actorUserId, decision, comment, DateTimeOffset.UtcNow));
        _domainEvents.Add(WorkflowStepDecidedEvent.Create(Id, step.StepId, actorUserId, decision));

        if (decision == WorkflowDecision.Reject)
        {
            Status = WorkflowInstanceStatus.Rejected;
            CurrentStepStartedAt = null;
            _domainEvents.Add(WorkflowCompletedEvent.Create(Id, BusinessObjectId, BusinessObjectType, Status, comment));
            return;
        }

        if (step.Quorum == ApprovalQuorum.All)
        {
            if (!_approvedByStep.TryGetValue(step.StepId, out var approvedSoFar))
            {
                approvedSoFar = new HashSet<string>();
                _approvedByStep[step.StepId] = approvedSoFar;
            }

            approvedSoFar.Add(actorUserId);

            var requiredForStep = _requiredApproversByStep.TryGetValue(step.StepId, out var reqSet) ? reqSet : new HashSet<string>();
            if (!approvedSoFar.IsSupersetOf(requiredForStep))
            {
                return; // still waiting on other required approvers for this step
            }
        }

        AdvanceToNextStep();
    }

    /// <summary>Withdraws a running instance (e.g. the underlying document was cancelled) — a distinct
    /// path from Reject, which happens because an approver actively declined.</summary>
    public void Cancel(string actorUserId, string reason)
    {
        if (Status != WorkflowInstanceStatus.Running)
        {
            throw new InvalidOperationException($"Workflow instance {Id} is not running (status: {Status}).");
        }

        Status = WorkflowInstanceStatus.Cancelled;
        CurrentStepStartedAt = null;
        _domainEvents.Add(WorkflowCompletedEvent.Create(Id, BusinessObjectId, BusinessObjectType, Status, reason));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void AdvanceToNextStep()
    {
        CurrentStepIndex++;

        if (CurrentStepIndex >= ApplicableSteps.Count)
        {
            Status = WorkflowInstanceStatus.Approved;
            CurrentStepStartedAt = null;
            _domainEvents.Add(WorkflowCompletedEvent.Create(Id, BusinessObjectId, BusinessObjectType, Status));
        }
        else
        {
            CurrentStepStartedAt = DateTimeOffset.UtcNow;
        }
    }
}
