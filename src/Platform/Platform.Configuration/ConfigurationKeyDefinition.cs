namespace Platform.Configuration;

/// <summary>
/// Declares one configurable item: what it's for, which levels it may be overridden at, and its
/// System-level default. In production these are maintained through an admin UI by a functional
/// consultant (docs/architecture/04-data-and-api.md #3), not hard-coded per module — this type is the
/// shape that configuration fills in.
/// </summary>
public sealed record ConfigurationKeyDefinition(
    string Key,
    string Description,
    IReadOnlyCollection<ConfigurationLevel> AllowedLevels,
    string? DefaultValue = null);
