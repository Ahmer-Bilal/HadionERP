using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Platform.Api;

/// <summary>
/// Parses the OData-style query parameters ($filter, $select, $orderby, $top, $skip, $count) from a
/// request's query string into a typed object (docs/architecture/05-data-and-api.md #2.1). This is the
/// standard input every List Report endpoint receives — consistent, predictable, and matches what
/// integrators expect coming from SAP/Dynamics backgrounds.
///
/// For Phase 0 this parses and VALIDATES the parameters. The $filter EXPRESSION ENGINE (parsing
/// "$filter=Amount gt 1000" into a real predicate) is deferred — it needs a proper grammar (OData ABNF /
/// ANTLR) and a real list endpoint to drive it; the structure + validation is built now so the contract is
/// stable when that lands. $orderby and $select are captured as raw strings for the consumer to interpret;
/// they're deliberately not parsed into expression trees yet.
/// </summary>
public sealed record ODataQuery(
    int Top,
    int Skip,
    string? OrderBy,
    string? Filter,
    IReadOnlyList<string> Select,
    bool Count)
{
    /// <summary>Default page size when $top is not specified — keeps list endpoints from returning
    /// unbounded result sets. A real deployment makes this configurable per endpoint.</summary>
    public const int DefaultTop = 50;

    /// <summary>Maximum page size — $top above this is clamped, so a caller can't request 10 million rows.</summary>
    public const int MaxTop = 1000;

    /// <summary>Parse from an HttpContext's query string. Returns null if no OData parameters are present
    /// (a plain GET with no query params). Throws <see cref="ArgumentException"/> on invalid values
    /// (negative $top, non-numeric, etc.) — the controller catches this and returns a 400 error envelope.</summary>
    public static ODataQuery Parse(IQueryCollection query)
    {
        var hasTop = query.TryGetValue("$top", out var topRaw);
        var hasSkip = query.TryGetValue("$skip", out var skipRaw);
        query.TryGetValue("$orderby", out var orderBy);
        query.TryGetValue("$filter", out var filter);
        var hasSelect = query.TryGetValue("$select", out var selectRaw);
        var hasCount = query.TryGetValue("$count", out var countRaw);

        // If none of the OData parameters are present, this isn't a query endpoint call — return defaults.
        if (!hasTop && !hasSkip && StringValues.IsNullOrEmpty(orderBy) && StringValues.IsNullOrEmpty(filter)
            && !hasSelect && !hasCount)
        {
            return new ODataQuery(DefaultTop, 0, null, null, Array.Empty<string>(), false);
        }

        var top = hasTop ? ParseNonNegativeInt(topRaw.ToString(), "$top") : DefaultTop;
        if (top > MaxTop)
        {
            top = MaxTop;
        }

        var skip = hasSkip ? ParseNonNegativeInt(skipRaw.ToString(), "$skip") : 0;

        var select = hasSelect && !StringValues.IsNullOrEmpty(selectRaw)
            ? selectRaw.ToString().Split(new[] { ',' }).Select(s => s.Trim()).Where(s => s.Length > 0).ToList()
            : (IReadOnlyList<string>)Array.Empty<string>();

        var count = hasCount && bool.TryParse(countRaw.ToString(), out var c) && c;

        return new ODataQuery(
            top,
            skip,
            StringValues.IsNullOrEmpty(orderBy) ? null : orderBy.ToString(),
            StringValues.IsNullOrEmpty(filter) ? null : filter.ToString(),
            select,
            count);
    }

    private static int ParseNonNegativeInt(string raw, string paramName)
    {
        if (!int.TryParse(raw, out var value))
        {
            throw new ArgumentException($"${paramName} must be a non-negative integer, got '{raw}'.");
        }

        if (value < 0)
        {
            throw new ArgumentException($"${paramName} must be non-negative, got {value}.");
        }

        return value;
    }
}
