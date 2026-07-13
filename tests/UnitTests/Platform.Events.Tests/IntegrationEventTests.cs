namespace Platform.Events.Tests;

public class IntegrationEventTests
{
    private sealed record PurchaseOrderApprovedPayload(string PurchaseOrderNumber, decimal Amount);

    [Fact]
    public void Payload_round_trips_through_JSON()
    {
        var integrationEvent = IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1",
            new PurchaseOrderApprovedPayload("PROC-PO-2026-000123", 75000m));

        var payload = integrationEvent.DeserializePayload<PurchaseOrderApprovedPayload>();

        Assert.Equal("PROC-PO-2026-000123", payload!.PurchaseOrderNumber);
        Assert.Equal(75000m, payload.Amount);
        Assert.Equal("Procurement.PurchaseOrderApproved.v1", integrationEvent.EventType);
    }

    [Fact]
    public void Each_created_event_gets_a_unique_id_and_a_UTC_timestamp()
    {
        var first = IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1", new { Amount = 1 });
        var second = IntegrationEvent.Create("Procurement.PurchaseOrderApproved.v1", new { Amount = 2 });

        Assert.NotEqual(first.EventId, second.EventId);
        Assert.Equal(TimeSpan.Zero, first.OccurredAt.Offset);
    }
}
