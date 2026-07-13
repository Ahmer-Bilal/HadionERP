namespace Platform.Configuration.FeatureFlags;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IConfigurationResolver _resolver;

    public FeatureFlagService(IConfigurationResolver resolver)
    {
        _resolver = resolver;
    }

    public bool IsEnabled(string flagKey, ConfigurationContext context)
    {
        var value = _resolver.Resolve(flagKey, context);
        return bool.TryParse(value, out var enabled) && enabled;
    }
}
