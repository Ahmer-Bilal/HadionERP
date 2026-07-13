namespace Platform.Events.Tests;

public class InMemoryEventBusTests
{
    [Fact]
    public async Task Publishing_invokes_all_subscribed_handlers_for_that_event_type()
    {
        var bus = new InMemoryEventBus();
        var received1 = new List<IntegrationEvent>();
        var received2 = new List<IntegrationEvent>();

        bus.Subscribe("Procurement.PurchaseOrderApproved.v1", (e, _) => { received1.Add(e); return Task.CompletedTask; });
        bus.Subscribe("Procurement.PurchaseOrderApproved.v1", (e, _) => { received2.Add(e); return Task.CompletedTask; });

        var integrationEvent = IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1", new { Amount = 100 });
        await bus.PublishAsync(integrationEvent);

        Assert.Single(received1);
        Assert.Single(received2);
        Assert.Equal(integrationEvent.EventId, received1[0].EventId);
    }

    [Fact]
    public async Task Publishing_an_event_type_with_no_subscribers_does_not_throw()
    {
        var bus = new InMemoryEventBus();

        await bus.PublishAsync(IntegrationEvent.Create("Nobody.IsListening.v1", new { }));
    }

    [Fact]
    public async Task Handlers_only_receive_events_for_the_event_type_they_subscribed_to()
    {
        var bus = new InMemoryEventBus();
        var received = new List<IntegrationEvent>();
        bus.Subscribe("Procurement.PurchaseOrderApproved.v1", (e, _) => { received.Add(e); return Task.CompletedTask; });

        await bus.PublishAsync(IntegrationEvent.Create("Finance.InvoicePosted.v1", new { }));

        Assert.Empty(received);
    }

    [Fact]
    public async Task Disposing_the_subscription_stops_future_deliveries()
    {
        var bus = new InMemoryEventBus();
        var received = new List<IntegrationEvent>();
        var subscription = bus.Subscribe("Procurement.PurchaseOrderApproved.v1", (e, _) => { received.Add(e); return Task.CompletedTask; });

        subscription.Dispose();
        await bus.PublishAsync(IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1", new { }));

        Assert.Empty(received);
    }
}
