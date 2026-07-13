namespace Platform.Events.Outbox;

/// <summary>
/// Drains pending outbox messages and publishes each to the event bus, marking it published on success.
/// Deliberately just the relay LOGIC, not a scheduler: the actual "run this every few seconds" job is a
/// hosted background service (Gateway.Api concern, not built yet — same deferral pattern as
/// Platform.Workflow's escalation scan). If publishing a given message throws, it stays pending and will
/// be retried on the next relay run — this is what makes the outbox pattern durable against a briefly
/// unavailable bus.
/// </summary>
public sealed class OutboxRelay
{
    private readonly IOutboxStore _outboxStore;
    private readonly IEventBus _eventBus;

    public OutboxRelay(IOutboxStore outboxStore, IEventBus eventBus)
    {
        _outboxStore = outboxStore;
        _eventBus = eventBus;
    }

    public async Task RelayPendingAsync(CancellationToken cancellationToken = default)
    {
        foreach (var message in _outboxStore.GetPending())
        {
            await _eventBus.PublishAsync(message.ToIntegrationEvent(), cancellationToken);
            _outboxStore.MarkPublished(message.Id, DateTimeOffset.UtcNow);
        }
    }
}
