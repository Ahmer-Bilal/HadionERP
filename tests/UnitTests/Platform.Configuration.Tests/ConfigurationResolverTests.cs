namespace Platform.Configuration.Tests;

public class ConfigurationResolverTests
{
    private static ConfigurationResolver BuildResolver(ConfigurationKeyDefinition definition, out InMemoryConfigurationStore store)
    {
        var catalog = new InMemoryConfigurationCatalog();
        catalog.Register(definition);
        store = new InMemoryConfigurationStore();
        return new ConfigurationResolver(catalog, store);
    }

    [Fact]
    public void Falls_back_to_the_registered_default_when_nothing_is_overridden()
    {
        var definition = new ConfigurationKeyDefinition(
            "Procurement.NumberFormat", "PO numbering format",
            new[] { ConfigurationLevel.System, ConfigurationLevel.Company }, DefaultValue: "PROC-PO-{Year}-{Seq}");
        var resolver = BuildResolver(definition, out _);

        var value = resolver.Resolve("Procurement.NumberFormat", new ConfigurationContext(CompanyId: "C001"));

        Assert.Equal("PROC-PO-{Year}-{Seq}", value);
    }

    [Fact]
    public void A_company_level_override_wins_over_the_system_default()
    {
        var definition = new ConfigurationKeyDefinition(
            "Procurement.NumberFormat", "PO numbering format",
            new[] { ConfigurationLevel.System, ConfigurationLevel.Company }, DefaultValue: "PROC-PO-{Year}-{Seq}");
        var resolver = BuildResolver(definition, out _);

        resolver.SetValue("Procurement.NumberFormat", ConfigurationLevel.Company,
            new ConfigurationContext(CompanyId: "C001"), "C001-PO-{Seq}");

        var forC001 = resolver.Resolve("Procurement.NumberFormat", new ConfigurationContext(CompanyId: "C001"));
        var forC002 = resolver.Resolve("Procurement.NumberFormat", new ConfigurationContext(CompanyId: "C002"));

        Assert.Equal("C001-PO-{Seq}", forC001);
        Assert.Equal("PROC-PO-{Year}-{Seq}", forC002); // a different company still gets the default
    }

    [Fact]
    public void The_most_specific_level_present_in_context_wins_over_less_specific_ones()
    {
        var definition = new ConfigurationKeyDefinition(
            "UI.Density", "Table row density", new[] { ConfigurationLevel.Company, ConfigurationLevel.User }, DefaultValue: "comfortable");
        var resolver = BuildResolver(definition, out _);

        var context = new ConfigurationContext(CompanyId: "C001", UserId: "u.ahmer");
        resolver.SetValue("UI.Density", ConfigurationLevel.Company, context, "compact");
        resolver.SetValue("UI.Density", ConfigurationLevel.User, context, "spacious");

        Assert.Equal("spacious", resolver.Resolve("UI.Density", context));
        // A different user at the same company still sees the company-level override.
        Assert.Equal("compact", resolver.Resolve("UI.Density", context with { UserId = "u.someoneelse" }));
    }

    [Fact]
    public void Setting_at_a_level_the_key_does_not_allow_throws()
    {
        var definition = new ConfigurationKeyDefinition(
            "UI.Density", "Table row density", new[] { ConfigurationLevel.User }, DefaultValue: "comfortable");
        var resolver = BuildResolver(definition, out _);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            resolver.SetValue("UI.Density", ConfigurationLevel.Company, new ConfigurationContext(CompanyId: "C001"), "compact"));

        Assert.Contains("cannot be overridden", ex.Message);
    }

    [Fact]
    public void Setting_at_a_level_without_the_matching_id_in_context_throws()
    {
        var definition = new ConfigurationKeyDefinition(
            "Procurement.NumberFormat", "PO numbering format", new[] { ConfigurationLevel.Company }, DefaultValue: "X");
        var resolver = BuildResolver(definition, out _);

        Assert.Throws<ArgumentException>(() =>
            resolver.SetValue("Procurement.NumberFormat", ConfigurationLevel.Company, ConfigurationContext.SystemOnly, "Y"));
    }

    [Fact]
    public void Resolving_an_unregistered_key_throws()
    {
        var resolver = new ConfigurationResolver(new InMemoryConfigurationCatalog(), new InMemoryConfigurationStore());

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Nobody.RegisteredThis", ConfigurationContext.SystemOnly));
    }
}
