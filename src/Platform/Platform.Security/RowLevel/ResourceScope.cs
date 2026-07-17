namespace Platform.Security.RowLevel;

/// <summary>
/// The scoping attributes of one specific record being accessed (e.g. its CompanyId/BranchId/ProjectId —
/// docs/architecture/04-platform-services.md #2.3, docs/architecture/05-data-and-api.md #1.3). This is
/// the application-layer half of row-level security; Postgres Row Level Security policies are the
/// database-layer backstop, added when Infrastructure/database work begins.
/// </summary>
public sealed record ResourceScope(IReadOnlyDictionary<string, string> Attributes)
{
    public static ResourceScope Of(params (string Dimension, string Value)[] attributes) =>
        new(attributes.ToDictionary(a => a.Dimension, a => a.Value));
}
