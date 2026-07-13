namespace Platform.Configuration;

public sealed class ConfigurationResolver : IConfigurationResolver
{
    private static readonly ConfigurationLevel[] ResolutionOrder =
    {
        ConfigurationLevel.User,
        ConfigurationLevel.Branch,
        ConfigurationLevel.Company,
        ConfigurationLevel.Tenant,
        ConfigurationLevel.System
    };

    private readonly IConfigurationCatalog _catalog;
    private readonly IConfigurationStore _store;

    public ConfigurationResolver(IConfigurationCatalog catalog, IConfigurationStore store)
    {
        _catalog = catalog;
        _store = store;
    }

    public string? Resolve(string key, ConfigurationContext context)
    {
        var definition = RequireDefinition(key);

        foreach (var level in ResolutionOrder)
        {
            if (!definition.AllowedLevels.Contains(level))
            {
                continue;
            }

            var scopeId = ScopeIdFor(level, context);
            if (level != ConfigurationLevel.System && scopeId is null)
            {
                continue; // context doesn't specify this level — nothing to look up here
            }

            var value = _store.GetValue(key, level, scopeId);
            if (value is not null)
            {
                return value;
            }
        }

        return definition.DefaultValue;
    }

    public void SetValue(string key, ConfigurationLevel level, ConfigurationContext context, string value)
    {
        var definition = RequireDefinition(key);

        if (!definition.AllowedLevels.Contains(level))
        {
            throw new InvalidOperationException(
                $"Configuration key '{key}' cannot be overridden at level '{level}' — allowed levels: " +
                string.Join(", ", definition.AllowedLevels));
        }

        var scopeId = ScopeIdFor(level, context);
        if (level != ConfigurationLevel.System && scopeId is null)
        {
            throw new ArgumentException(
                $"Setting '{key}' at level '{level}' requires the corresponding id in the context.", nameof(context));
        }

        _store.SetValue(key, level, scopeId, value);
    }

    private ConfigurationKeyDefinition RequireDefinition(string key) =>
        _catalog.Resolve(key) ?? throw new InvalidOperationException($"Configuration key '{key}' is not registered.");

    private static string? ScopeIdFor(ConfigurationLevel level, ConfigurationContext context) => level switch
    {
        ConfigurationLevel.System => null,
        ConfigurationLevel.Tenant => context.TenantId,
        ConfigurationLevel.Company => context.CompanyId,
        ConfigurationLevel.Branch => context.BranchId,
        ConfigurationLevel.User => context.UserId,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
}
