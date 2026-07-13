using Platform.Events.Outbox;

namespace Platform.Events.Tests;

/// <summary>
/// Proves the whole architecture in one test: a module publishes an event, it's staged in the outbox
/// (not lost even if the bus were down), a relay drains the outbox to the bus, and a subscriber (playing
/// the part of a different module reacting to it — e.g. Finance reacting to Procurement's approval)
/// actually receives it. This is the concrete mechanism behind docs/architecture/03-platform-services.md
/// #3's example: "PO Approved -> Finance creates budget commitment."
/// </summary>
public class EndToEndPipelineTests
{
    private sealed record PurchaseOrderApprovedPayload(string PurchaseOrderNumber, decimal Amount);

    [Fact]
    public async Task Enqueue_then_relay_delivers_the_event_to_a_subscriber()
    {
        var catalog = new InMemoryEventCatalog();
        catalog.Register("Procurement.PurchaseOrderApproved.v1", "Raised when a PO completes approval.");

        var outboxStore = new InMemoryOutboxStore();
        var bus = new InMemoryEventBus();
        var publisher = new IntegrationEventPublisher(catalog, outboxStore);
        var relay = new OutboxRelay(outboxStore, bus);

        PurchaseOrderApprovedPayload? received = null;
        bus.Subscribe("Procurement.PurchaseOrderApproved.v1", (e, _) =>
        {
            received = e.DeserializePayload<PurchaseOrderApprovedPayload>();
            return Task.CompletedTask;
        });

        // Procurement's Application layer publishes — this only stages it, it does not touch the bus yet.
        publisher.Enqueue(IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1",
            new PurchaseOrderApprovedPayload("PROC-PO-2026-000123", 75000m)));

        Assert.Null(received); // not delivered until the relay actually runs
        Assert.Single(outboxStore.GetPending());

        // The relay (a scheduled background job in a real deployment) drains the outbox.
        await relay.RelayPendingAsync();

        Assert.NotNull(received);
        Assert.Equal("PROC-PO-2026-000123", received!.PurchaseOrderNumber);
        Assert.Equal(75000m, received.Amount);
        Assert.Empty(outboxStore.GetPending());
        Assert.True(outboxStore.GetAll().Single().IsPublished);
    }
}
