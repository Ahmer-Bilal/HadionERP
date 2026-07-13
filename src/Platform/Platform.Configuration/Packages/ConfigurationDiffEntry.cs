namespace Platform.Configuration.Packages;

public enum ConfigurationChangeType
{
    Added,
    Modified
}

/// <summary>One difference between a package and the current store — the "review step before applying
/// to Prod" the architecture calls for.</summary>
public sealed record ConfigurationDiffEntry(string Key, ConfigurationLevel Level, string? ScopeId, string? CurrentValue, string? IncomingValue, ConfigurationChangeType ChangeType);
