namespace Platform.Configuration;

/// <summary>Reference implementation of <see cref="IConfigurationStore"/> — a real deployment backs this
/// with a database table; this proves the storage contract first, same pattern as everywhere else in the
/// kernel.</summary>
public sealed class InMemoryConfigurationStore : IConfigurationStore
{
    private readonly Dictionary<(string Key, ConfigurationLevel Level, string? ScopeId), string> _values = new();

    public void SetValue(string key, ConfigurationLevel level, string? scopeId, string value) =>
        _values[(key, level, scopeId)] = value;

    public string? GetValue(string key, ConfigurationLevel level, string? scopeId) =>
        _values.GetValueOrDefault((key, level, scopeId));

    public IReadOnlyCollection<ConfigurationValueRecord> GetAll() =>
        _values.Select(kv => new ConfigurationValueRecord(kv.Key.Key, kv.Key.Level, kv.Key.ScopeId, kv.Value)).ToList();
}
