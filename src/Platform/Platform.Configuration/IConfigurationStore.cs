namespace Platform.Configuration;

/// <summary>Raw storage for configuration overrides — no validation, no defaulting, no hierarchy
/// walking (that's <see cref="IConfigurationResolver"/>'s job). Kept this thin so a database-backed
/// implementation later is a simple key-value table, not business logic.</summary>
public interface IConfigurationStore
{
    void SetValue(string key, ConfigurationLevel level, string? scopeId, string value);

    string? GetValue(string key, ConfigurationLevel level, string? scopeId);

    IReadOnlyCollection<ConfigurationValueRecord> GetAll();
}
