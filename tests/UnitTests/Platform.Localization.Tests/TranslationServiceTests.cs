using Platform.Localization.Translation;

namespace Platform.Localization.Tests;

public class TranslationServiceTests
{
    [Fact]
    public void Returns_requested_language_when_registered()
    {
        var service = new InMemoryTranslationService();
        service.RegisterDefault("Modules.Procurement.PurchaseOrder.Header", SupportedLanguage.English, "Purchase Order");
        service.RegisterDefault("Modules.Procurement.PurchaseOrder.Header", SupportedLanguage.Arabic, "أمر شراء");

        Assert.Equal("أمر شراء", service.Translate("Modules.Procurement.PurchaseOrder.Header", SupportedLanguage.Arabic));
        Assert.Equal("Purchase Order", service.Translate("Modules.Procurement.PurchaseOrder.Header", SupportedLanguage.English));
    }

    [Fact]
    public void Falls_back_to_English_when_requested_language_missing()
    {
        var service = new InMemoryTranslationService();
        service.RegisterDefault("Modules.Demo.Field", SupportedLanguage.English, "Demo Field");
        // No Arabic translation registered yet.

        Assert.Equal("Demo Field", service.Translate("Modules.Demo.Field", SupportedLanguage.Arabic));
    }

    [Fact]
    public void Returns_a_visibly_missing_marker_when_the_key_is_entirely_unregistered()
    {
        var service = new InMemoryTranslationService();

        Assert.Equal("[[Modules.Nonexistent.Key]]", service.Translate("Modules.Nonexistent.Key", SupportedLanguage.English));
    }

    [Fact]
    public void Tenant_override_takes_priority_over_the_module_default()
    {
        var service = new InMemoryTranslationService();
        service.RegisterDefault("Modules.Procurement.PurchaseOrder.Header", SupportedLanguage.English, "Purchase Order");
        service.RegisterTenantOverride("tenant-gfalcom", "Modules.Procurement.PurchaseOrder.Header", SupportedLanguage.English, "Purchase Request");

        Assert.Equal("Purchase Request",
            service.Translate("Modules.Procurement.PurchaseOrder.Header", SupportedLanguage.English, "tenant-gfalcom"));

        // A different tenant (or no tenant) still sees the module default.
        Assert.Equal("Purchase Order",
            service.Translate("Modules.Procurement.PurchaseOrder.Header", SupportedLanguage.English));
    }
}
