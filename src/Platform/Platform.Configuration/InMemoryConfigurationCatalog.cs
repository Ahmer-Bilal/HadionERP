namespace Platform.Configuration;

/// <summary>Reference implementation of <see cref="IConfigurationCatalog"/> — same swap-for-a-database-
/// backed-registry-later pattern as the rest of the kernel.</summary>
public sealed class InMemoryConfigurationCatalog : IConfigurationCatalog
{
    private readonly Dictionary<string, ConfigurationKeyDefinition> _definitions = new();

    public void Register(ConfigurationKeyDefinition definition) => _definitions[definition.Key] = definition;

    public ConfigurationKeyDefinition? Resolve(string key) => _definitions.GetValueOrDefault(key);

    public IReadOnlyCollection<ConfigurationKeyDefinition> GetAll() => _definitions.Values.ToList();
}
