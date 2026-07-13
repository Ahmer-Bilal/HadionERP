using Platform.Events.Outbox;

namespace Platform.Events;

public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IEventCatalog _catalog;
    private readonly IOutboxStore _outboxStore;

    public IntegrationEventPublisher(IEventCatalog catalog, IOutboxStore outboxStore)
    {
        _catalog = catalog;
        _outboxStore = outboxStore;
    }

    public void Enqueue(IntegrationEvent integrationEvent)
    {
        if (!_catalog.IsRegistered(integrationEvent.EventType))
        {
            throw new InvalidOperationException(
                $"Event type '{integrationEvent.EventType}' is not registered in the event catalog. " +
                "Register it (with its version) before publishing — see docs/architecture/03-platform-services.md #3.");
        }

        _outboxStore.Enqueue(integrationEvent);
    }
}
