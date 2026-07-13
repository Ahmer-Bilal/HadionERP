namespace Platform.Events.Outbox;

/// <summary>Reference implementation of <see cref="IOutboxStore"/> — a real deployment backs this with
/// an actual outbox table written in the same database transaction as the business change; this
/// in-memory version proves the enqueue/relay/mark-published mechanics first.</summary>
public sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, OutboxMessage> _messages = new();

    public void Enqueue(IntegrationEvent integrationEvent)
    {
        var message = new OutboxMessage(
            integrationEvent.EventId,
            integrationEvent.EventType,
            integrationEvent.PayloadJson,
            integrationEvent.CorrelationId,
            integrationEvent.OccurredAt,
            PublishedAt: null);

        lock (_lock)
        {
            _messages[message.Id] = message;
        }
    }

    public IReadOnlyList<OutboxMessage> GetPending()
    {
        lock (_lock)
        {
            return _messages.Values.Where(m => !m.IsPublished).ToList();
        }
    }

    public IReadOnlyList<OutboxMessage> GetAll()
    {
        lock (_lock)
        {
            return _messages.Values.ToList();
        }
    }

    public void MarkPublished(Guid id, DateTimeOffset publishedAt)
    {
        lock (_lock)
        {
            if (_messages.TryGetValue(id, out var message))
            {
                _messages[id] = message with { PublishedAt = publishedAt };
            }
        }
    }
}
