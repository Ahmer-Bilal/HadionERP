namespace Platform.Configuration;

/// <summary>
/// The override hierarchy every configurable item resolves through, most-specific first
/// (docs/architecture/05-data-and-api.md #3): System defaults → Tenant → Company → Branch → User. Not
/// every setting is overridable at every level — e.g. a UI density preference makes sense per-User but
/// not per-Branch, while a document numbering format makes sense per-Company but not per-User. See
/// <see cref="ConfigurationKeyDefinition.AllowedLevels"/>.
/// </summary>
public enum ConfigurationLevel
{
    System,
    Tenant,
    Company,
    Branch,
    User
}
