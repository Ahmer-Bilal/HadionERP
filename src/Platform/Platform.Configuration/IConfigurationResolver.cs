namespace Platform.Configuration;

/// <summary>
/// The single entry point modules use to read/write configuration — mirrors how
/// IAuthorizationService/IWorkflowEngine/IIntegrationEventPublisher are the single entry points for their
/// concerns. Walks the override hierarchy (docs/architecture/05-data-and-api.md #3) and enforces that a
/// key is only ever set at a level it declares itself overridable at.
/// </summary>
public interface IConfigurationResolver
{
    /// <summary>Resolves the effective value: the most specific level (per <paramref name="context"/>)
    /// that has an override, falling back through less specific levels, then to the key's registered
    /// default. Throws if the key isn't registered in the catalog.</summary>
    string? Resolve(string key, ConfigurationContext context);

    /// <summary>Sets an override at a specific level. Throws if the key doesn't allow that level, or if
    /// the context is missing the id that level needs (e.g. setting at Company without a CompanyId).</summary>
    void SetValue(string key, ConfigurationLevel level, ConfigurationContext context, string value);
}
