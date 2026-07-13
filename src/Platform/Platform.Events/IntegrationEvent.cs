using System.Text.Json;

namespace Platform.Events;

/// <summary>
/// A fact published for other modules/services to react to — crosses process/module boundaries, unlike
/// a Platform.Core.Events.IDomainEvent, which stays strictly in-process (docs/architecture/03-platform-services.md
/// #3). Nothing automatically turns a domain event into an integration event: a module's Application
/// layer deliberately decides which domain events also matter outside the module and translates those
/// explicitly — that decision is a design choice per event, not a generic bridge.
///
/// <see cref="EventType"/> follows the versioned contract naming convention "{Module}.{Event}.v{N}"
/// (e.g. "Procurement.PurchaseOrderApproved.v1") — see docs/architecture/03-platform-services.md #3 and
/// IEventCatalog, which is what enforces that only registered, versioned contracts get published.
/// </summary>
public sealed record IntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, string EventType, string PayloadJson, Guid? CorrelationId = null)
{
    public static IntegrationEvent Create<TPayload>(string eventType, TPayload payload, Guid? correlationId = null) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, eventType, JsonSerializer.Serialize(payload), correlationId);

    public TPayload? DeserializePayload<TPayload>() => JsonSerializer.Deserialize<TPayload>(PayloadJson);
}
