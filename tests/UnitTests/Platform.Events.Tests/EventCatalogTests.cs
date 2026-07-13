namespace Platform.Events.Tests;

public class EventCatalogTests
{
    [Fact]
    public void An_unregistered_event_type_is_not_registered()
    {
        var catalog = new InMemoryEventCatalog();

        Assert.False(catalog.IsRegistered("Procurement.PurchaseOrderApproved.v1"));
    }

    [Fact]
    public void A_registered_event_type_is_registered()
    {
        var catalog = new InMemoryEventCatalog();
        catalog.Register("Procurement.PurchaseOrderApproved.v1", "Raised when a PO completes approval.");

        Assert.True(catalog.IsRegistered("Procurement.PurchaseOrderApproved.v1"));
    }
}
