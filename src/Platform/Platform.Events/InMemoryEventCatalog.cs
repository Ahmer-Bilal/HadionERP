namespace Platform.Events;

/// <summary>Reference implementation of <see cref="IEventCatalog"/> — same "swap for a database-backed
/// registry later behind the same interface" pattern as the rest of the kernel.</summary>
public sealed class InMemoryEventCatalog : IEventCatalog
{
    private readonly Dictionary<string, string> _registeredEvents = new();

    public void Register(string eventType, string description) => _registeredEvents[eventType] = description;

    public bool IsRegistered(string eventType) => _registeredEvents.ContainsKey(eventType);
}
