namespace Platform.Configuration;

/// <summary>One stored override: <paramref name="Key"/> at <paramref name="Level"/>, scoped to
/// <paramref name="ScopeId"/> (null for System, since there's only one). This is the unit configuration
/// packages (Packages/ConfigurationPackage.cs) export/import.</summary>
public sealed record ConfigurationValueRecord(string Key, ConfigurationLevel Level, string? ScopeId, string Value);
