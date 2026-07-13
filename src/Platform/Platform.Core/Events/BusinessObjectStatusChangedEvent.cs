namespace Platform.Core.Events;

/// <summary>
/// Raised automatically by <see cref="Platform.Core.BusinessObject"/> on every lifecycle transition —
/// this is the "every transition emits a domain event automatically" guarantee from
/// docs/architecture/02-business-object-model.md §1.1. A module MAY additionally raise its own typed
/// event (e.g. PurchaseOrderApprovedEvent) alongside this one; it does not have to, because this generic
/// event already gives Audit/Workflow/other listeners everything they need to react.
/// </summary>
public sealed record BusinessObjectStatusChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BusinessObjectId,
    string BusinessObjectType,
    BusinessObjectStatus From,
    BusinessObjectStatus To,
    BusinessObjectTransition Transition,
    string Actor) : IDomainEvent
{
    public static BusinessObjectStatusChangedEvent Create(
        Guid businessObjectId,
        string businessObjectType,
        BusinessObjectStatus from,
        BusinessObjectStatus to,
        BusinessObjectTransition transition,
        string actor)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, businessObjectId, businessObjectType, from, to, transition, actor);
}
