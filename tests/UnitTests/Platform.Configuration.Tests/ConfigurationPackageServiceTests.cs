using Platform.Configuration.Packages;

namespace Platform.Configuration.Tests;

public class ConfigurationPackageServiceTests
{
    private static (ConfigurationPackageService Service, InMemoryConfigurationStore Store, InMemoryConfigurationCatalog Catalog) Build()
    {
        var catalog = new InMemoryConfigurationCatalog();
        catalog.Register(new ConfigurationKeyDefinition(
            "Procurement.NumberFormat", "PO numbering format",
            new[] { ConfigurationLevel.Company }, DefaultValue: "PO-{Seq}"));
        var store = new InMemoryConfigurationStore();
        return (new ConfigurationPackageService(catalog, store), store, catalog);
    }

    [Fact]
    public void Export_captures_the_current_store_contents()
    {
        var (service, store, _) = Build();
        store.SetValue("Procurement.NumberFormat", ConfigurationLevel.Company, "C001", "C001-PO-{Seq}");

        var package = service.Export("dev-snapshot", "1.0");

        var value = Assert.Single(package.Values);
        Assert.Equal("C001-PO-{Seq}", value.Value);
    }

    [Fact]
    public void Diff_reports_added_and_modified_entries_without_changing_anything()
    {
        var (service, store, _) = Build();
        store.SetValue("Procurement.NumberFormat", ConfigurationLevel.Company, "C001", "OLD-FORMAT");

        var incomingPackage = new ConfigurationPackage("promote-to-uat", "1.1", DateTimeOffset.UtcNow, new[]
        {
            new ConfigurationValueRecord("Procurement.NumberFormat", ConfigurationLevel.Company, "C001", "NEW-FORMAT"),
            new ConfigurationValueRecord("Procurement.NumberFormat", ConfigurationLevel.Company, "C002", "C002-FORMAT"),
        });

        var diffs = service.Diff(incomingPackage);

        Assert.Equal(2, diffs.Count);
        Assert.Contains(diffs, d => d.ScopeId == "C001" && d.ChangeType == ConfigurationChangeType.Modified && d.CurrentValue == "OLD-FORMAT");
        Assert.Contains(diffs, d => d.ScopeId == "C002" && d.ChangeType == ConfigurationChangeType.Added && d.CurrentValue == null);
        // Diff must not mutate the store.
        Assert.Equal("OLD-FORMAT", store.GetValue("Procurement.NumberFormat", ConfigurationLevel.Company, "C001"));
    }

    [Fact]
    public void Import_applies_every_value_in_the_package()
    {
        var (service, store, _) = Build();
        var package = new ConfigurationPackage("promote-to-uat", "1.1", DateTimeOffset.UtcNow, new[]
        {
            new ConfigurationValueRecord("Procurement.NumberFormat", ConfigurationLevel.Company, "C001", "NEW-FORMAT"),
        });

        service.Import(package);

        Assert.Equal("NEW-FORMAT", store.GetValue("Procurement.NumberFormat", ConfigurationLevel.Company, "C001"));
    }

    [Fact]
    public void Import_rejects_a_key_that_is_not_registered_in_this_environment()
    {
        var (service, _, _) = Build();
        var package = new ConfigurationPackage("from-a-newer-env", "2.0", DateTimeOffset.UtcNow, new[]
        {
            new ConfigurationValueRecord("Some.FutureKey", ConfigurationLevel.System, null, "value"),
        });

        var ex = Assert.Throws<InvalidOperationException>(() => service.Import(package));
        Assert.Contains("not a registered configuration key", ex.Message);
    }

    [Fact]
    public void Import_rejects_a_level_the_key_does_not_allow_in_this_environment()
    {
        var (service, _, _) = Build();
        var package = new ConfigurationPackage("mismatched-env", "1.0", DateTimeOffset.UtcNow, new[]
        {
            new ConfigurationValueRecord("Procurement.NumberFormat", ConfigurationLevel.User, "u.someone", "value"),
        });

        var ex = Assert.Throws<InvalidOperationException>(() => service.Import(package));
        Assert.Contains("not an allowed level", ex.Message);
    }
}
