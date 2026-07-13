namespace Platform.Configuration;

/// <summary>The registered set of configurable items — same "registered contract, not ad-hoc" pattern
/// as Platform.Security's ISecurityCatalog and Platform.Events' IEventCatalog.</summary>
public interface IConfigurationCatalog
{
    void Register(ConfigurationKeyDefinition definition);
    ConfigurationKeyDefinition? Resolve(string key);
    IReadOnlyCollection<ConfigurationKeyDefinition> GetAll();
}
