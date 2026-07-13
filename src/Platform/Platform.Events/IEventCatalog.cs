namespace Platform.Events;

/// <summary>
/// The registered set of integration event contracts — "every integration event is a versioned,
/// schema-registered contract published in each module's Contracts package" (docs/architecture/03-platform-services.md
/// #3). This is what stops an unversioned, undocumented event type from ever being published: a module
/// registers its contracts once (e.g. at startup), and <see cref="IIntegrationEventPublisher"/> checks
/// against this before staging anything into the outbox.
/// </summary>
public interface IEventCatalog
{
    void Register(string eventType, string description);
    bool IsRegistered(string eventType);
}
