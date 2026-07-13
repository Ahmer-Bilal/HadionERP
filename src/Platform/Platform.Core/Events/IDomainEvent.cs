namespace Platform.Core.Events;

/// <summary>
/// A fact that happened inside a Business Object, in-process. Domain events never cross a module's
/// process boundary — that is what integration events (Platform.Events, not yet built) are for.
/// See docs/architecture/03-platform-services.md §3.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
