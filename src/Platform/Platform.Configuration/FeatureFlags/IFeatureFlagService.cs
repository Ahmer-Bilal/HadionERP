namespace Platform.Configuration.FeatureFlags;

/// <summary>
/// Gates incomplete/opt-in functionality per tenant (docs/architecture/05-data-and-api.md #3.4). A
/// feature flag IS a configuration value (a boolean one) resolved through the exact same override
/// hierarchy as any other setting — this is a thin, purpose-named wrapper over
/// <see cref="IConfigurationResolver"/>, not a second parallel system.
/// </summary>
public interface IFeatureFlagService
{
    bool IsEnabled(string flagKey, ConfigurationContext context);
}
