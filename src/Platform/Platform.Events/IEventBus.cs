namespace Platform.Events;

/// <summary>
/// Abstracts the messaging transport (RabbitMQ by default, pluggable to Azure Service Bus/Kafka in the
/// cloud — ADR-5) behind one interface, so swapping the transport never touches module code. Modules
/// never call this directly for publishing — that always goes through the outbox
/// (<see cref="IIntegrationEventPublisher"/>) so an event is never lost even if the bus is briefly down;
/// this interface is what the outbox relay calls, and what a module subscribes against to react to
/// other modules' events.
/// </summary>
public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    /// <summary>Registers a handler for a given event type; returns a disposable that unsubscribes it.</summary>
    IDisposable Subscribe(string eventType, Func<IntegrationEvent, CancellationToken, Task> handler);
}
