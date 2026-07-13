using Platform.Core.Events;

namespace Platform.Core;

/// <summary>
/// The contract every Business Object in the platform implements. See
/// docs/architecture/02-business-object-model.md §1 for what each part is for.
/// </summary>
public interface IBusinessObject
{
    Guid Id { get; }
    string? DocumentNumber { get; }
    BusinessObjectStatus Status { get; }

    /// <summary>Optimistic-concurrency token — incremented on every persisted change.</summary>
    long RowVersion { get; }

    string CreatedBy { get; }
    DateTimeOffset CreatedAt { get; }
    string? ModifiedBy { get; }
    DateTimeOffset? ModifiedAt { get; }

    ExtensionFieldBag ExtensionFields { get; }
    IReadOnlyCollection<BusinessObjectReference> Relations { get; }

    /// <summary>
    /// Events raised since the last time <see cref="ClearDomainEvents"/> was called. Infrastructure
    /// drains this after a successful save and hands the events to Platform.Events for publishing
    /// (outbox pattern, doc 03 §3) — Domain code never publishes directly.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
