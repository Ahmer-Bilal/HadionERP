using Platform.Configuration.FeatureFlags;

namespace Platform.Configuration.Tests;

public class FeatureFlagServiceTests
{
    private static (FeatureFlagService Service, ConfigurationResolver Resolver) Build()
    {
        var catalog = new InMemoryConfigurationCatalog();
        catalog.Register(new ConfigurationKeyDefinition(
            "Features.NewReportingDashboard", "Opt-in new dashboard",
            new[] { ConfigurationLevel.System, ConfigurationLevel.Tenant }, DefaultValue: "false"));
        var resolver = new ConfigurationResolver(catalog, new InMemoryConfigurationStore());
        return (new FeatureFlagService(resolver), resolver);
    }

    [Fact]
    public void Defaults_to_disabled_when_unset()
    {
        var (service, _) = Build();

        Assert.False(service.IsEnabled("Features.NewReportingDashboard", ConfigurationContext.SystemOnly));
    }

    [Fact]
    public void Can_be_enabled_for_one_tenant_without_affecting_others()
    {
        var (service, resolver) = Build();
        resolver.SetValue("Features.NewReportingDashboard", ConfigurationLevel.Tenant,
            new ConfigurationContext(TenantId: "tenant-gfalcom"), "true");

        Assert.True(service.IsEnabled("Features.NewReportingDashboard", new ConfigurationContext(TenantId: "tenant-gfalcom")));
        Assert.False(service.IsEnabled("Features.NewReportingDashboard", new ConfigurationContext(TenantId: "tenant-other")));
    }
}
