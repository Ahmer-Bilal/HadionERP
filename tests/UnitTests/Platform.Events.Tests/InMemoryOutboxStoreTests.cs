using Platform.Events.Outbox;

namespace Platform.Events.Tests;

public class InMemoryOutboxStoreTests
{
    [Fact]
    public void An_enqueued_message_is_pending_until_marked_published()
    {
        var store = new InMemoryOutboxStore();
        var integrationEvent = IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1", new { Amount = 100 });
        store.Enqueue(integrationEvent);

        var pending = store.GetPending();

        var message = Assert.Single(pending);
        Assert.Equal(integrationEvent.EventId, message.Id);
        Assert.False(message.IsPublished);
    }

    [Fact]
    public void Marking_published_removes_it_from_pending_but_keeps_it_in_GetAll()
    {
        var store = new InMemoryOutboxStore();
        var integrationEvent = IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1", new { Amount = 100 });
        store.Enqueue(integrationEvent);

        store.MarkPublished(integrationEvent.EventId, DateTimeOffset.UtcNow);

        Assert.Empty(store.GetPending());
        var all = Assert.Single(store.GetAll());
        Assert.True(all.IsPublished);
    }
}
