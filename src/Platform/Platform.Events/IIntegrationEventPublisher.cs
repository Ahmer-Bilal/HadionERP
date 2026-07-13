namespace Platform.Events;

/// <summary>
/// The single entry point modules use to publish an integration event — mirrors how
/// IAuthorizationService/IWorkflowEngine are the single entry points for their concerns. A module never
/// calls IEventBus directly to publish; this always stages into the outbox first, so an event is never
/// lost even if the bus is briefly unavailable.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Stages the event for publishing. Throws if its EventType isn't registered in the
    /// IEventCatalog — an unversioned, undocumented event type is never allowed to be published.</summary>
    void Enqueue(IntegrationEvent integrationEvent);
}
