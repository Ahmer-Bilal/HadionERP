namespace Platform.Configuration.Packages;

public sealed class ConfigurationPackageService : IConfigurationPackageService
{
    private readonly IConfigurationCatalog _catalog;
    private readonly IConfigurationStore _store;

    public ConfigurationPackageService(IConfigurationCatalog catalog, IConfigurationStore store)
    {
        _catalog = catalog;
        _store = store;
    }

    public ConfigurationPackage Export(string name, string version) =>
        new(name, version, DateTimeOffset.UtcNow, _store.GetAll());

    public IReadOnlyList<ConfigurationDiffEntry> Diff(ConfigurationPackage package)
    {
        var current = _store.GetAll().ToDictionary(v => (v.Key, v.Level, v.ScopeId), v => v.Value);
        var diffs = new List<ConfigurationDiffEntry>();

        foreach (var incoming in package.Values)
        {
            var lookupKey = (incoming.Key, incoming.Level, incoming.ScopeId);

            if (!current.TryGetValue(lookupKey, out var currentValue))
            {
                diffs.Add(new ConfigurationDiffEntry(incoming.Key, incoming.Level, incoming.ScopeId, null, incoming.Value, ConfigurationChangeType.Added));
            }
            else if (currentValue != incoming.Value)
            {
                diffs.Add(new ConfigurationDiffEntry(incoming.Key, incoming.Level, incoming.ScopeId, currentValue, incoming.Value, ConfigurationChangeType.Modified));
            }
        }

        return diffs;
    }

    public void Import(ConfigurationPackage package)
    {
        foreach (var value in package.Values)
        {
            var definition = _catalog.Resolve(value.Key)
                ?? throw new InvalidOperationException(
                    $"Cannot import '{value.Key}' — it is not a registered configuration key in this environment.");

            if (!definition.AllowedLevels.Contains(value.Level))
            {
                throw new InvalidOperationException(
                    $"Cannot import '{value.Key}' at level '{value.Level}' — not an allowed level in this environment.");
            }

            _store.SetValue(value.Key, value.Level, value.ScopeId, value.Value);
        }
    }
}
