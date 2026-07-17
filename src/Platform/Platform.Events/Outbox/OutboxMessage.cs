namespace Platform.Events.Outbox;

/// <summary>
/// One staged, not-yet-published integration event — the outbox pattern's whole point
/// (docs/architecture/04-platform-services.md #3): staging happens in the same transaction as the
/// business change (once a real database exists — this in-memory version proves the mechanism first),
/// so an event is never lost even if the bus is briefly unavailable when the relay tries to publish it.
/// </summary>
public sealed record OutboxMessage(Guid Id, string EventType, string PayloadJson, Guid? CorrelationId, DateTimeOffset OccurredAt, DateTimeOffset? PublishedAt)
{
    public bool IsPublished => PublishedAt is not null;

    public IntegrationEvent ToIntegrationEvent() => new(Id, OccurredAt, EventType, PayloadJson, CorrelationId);
}
