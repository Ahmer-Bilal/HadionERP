namespace Platform.Configuration.Packages;

public interface IConfigurationPackageService
{
    ConfigurationPackage Export(string name, string version);

    /// <summary>What would change if <paramref name="package"/> were imported, without applying anything —
    /// the review step before promoting to Prod.</summary>
    IReadOnlyList<ConfigurationDiffEntry> Diff(ConfigurationPackage package);

    /// <summary>Applies every value in the package. Throws if a value's key isn't registered, or isn't
    /// allowed at that level, in THIS environment's catalog — importing a package built against a
    /// different (e.g. older) set of key definitions fails loudly rather than silently applying stale
    /// configuration.</summary>
    void Import(ConfigurationPackage package);
}
