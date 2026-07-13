using Platform.Events.Outbox;

namespace Platform.Events.Tests;

public class IntegrationEventPublisherTests
{
    [Fact]
    public void Rejects_an_event_type_that_is_not_registered_in_the_catalog()
    {
        var publisher = new IntegrationEventPublisher(new InMemoryEventCatalog(), new InMemoryOutboxStore());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            publisher.Enqueue(IntegrationEvent.Create("Nobody.RegisteredThis.v1", new { })));

        Assert.Contains("not registered", ex.Message);
    }

    [Fact]
    public void Stages_a_registered_event_type_into_the_outbox()
    {
        var catalog = new InMemoryEventCatalog();
        catalog.Register("Procurement.PurchaseOrderApproved.v1", "Raised when a PO completes approval.");
        var outboxStore = new InMemoryOutboxStore();
        var publisher = new IntegrationEventPublisher(catalog, outboxStore);

        var integrationEvent = IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1", new { Amount = 100 });
        publisher.Enqueue(integrationEvent);

        var pending = Assert.Single(outboxStore.GetPending());
        Assert.Equal(integrationEvent.EventId, pending.Id);
    }
}
