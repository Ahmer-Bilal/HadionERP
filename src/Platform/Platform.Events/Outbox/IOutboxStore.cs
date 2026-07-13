namespace Platform.Events.Outbox;

public interface IOutboxStore
{
    void Enqueue(IntegrationEvent integrationEvent);

    IReadOnlyList<OutboxMessage> GetPending();

    void MarkPublished(Guid id, DateTimeOffset publishedAt);

    /// <summary>All messages regardless of publish state — for status/observability surfaces, not for
    /// deciding what to relay (use <see cref="GetPending"/> for that).</summary>
    IReadOnlyList<OutboxMessage> GetAll();
}
