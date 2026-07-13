namespace Platform.Api;

/// <summary>
/// The standard envelope every List Report endpoint returns (docs/architecture/04-data-and-api.md #2.1 —
/// OData-inspired query conventions). A consistent shape so the frontend and external integrators always
/// know how to page through a list, regardless of which module's endpoint they're calling.
///
/// <see cref="TotalCount"/> is the count of the FULL result set (before paging) — the frontend needs this
/// to render "showing 1–25 of 312". <see cref="Items"/> is the paged slice requested via $top/$skip.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Skip,
    int Top)
{
    /// <summary>Builds a paged result from a full in-memory set, applying $top/$skip. This is the helper
    /// a module's list endpoint calls when its data source is already materialized (e.g. an in-memory
    /// reference store). A real deployment with a database applies TOP/OFFSET in SQL instead, but produces
    /// the same envelope.</summary>
    public static PagedResult<T> From(IReadOnlyList<T> fullSet, int skip, int top)
    {
        var paged = top > 0
            ? fullSet.Skip(skip).Take(top).ToList()
            : fullSet.Skip(skip).ToList();

        return new PagedResult<T>(paged, fullSet.Count, skip, top);
    }
}
