namespace Platform.Configuration.Packages;

/// <summary>
/// A versioned, exportable/importable snapshot of configuration values — the mechanism behind promoting
/// configuration between environments (docs/architecture/05-data-and-api.md #3.4: "packaged into
/// versioned, exportable/importable configuration packages... to promote Dev → Test → UAT → Prod
/// deterministically, with a diff/review step before applying to Prod").
/// </summary>
public sealed record ConfigurationPackage(string Name, string Version, DateTimeOffset ExportedAt, IReadOnlyCollection<ConfigurationValueRecord> Values);
