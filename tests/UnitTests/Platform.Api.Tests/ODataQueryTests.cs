using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Platform.Api.Tests;

/// <summary>
/// Proves <see cref="ODataQuery.Parse"/> correctly extracts $top/$skip/$orderby/$select/$count from a
/// query string, applies defaults when absent, clamps to the max page size, and rejects invalid values.
/// This is the contract every List Report endpoint will rely on.
/// </summary>
public class ODataQueryTests
{
    private static IQueryCollection Query(params (string Key, string Value)[] pairs) =>
        new QueryCollection(pairs.ToDictionary(p => p.Key, p => (StringValues)p.Value));

    [Fact]
    public void No_parameters_returns_defaults()
    {
        var query = ODataQuery.Parse(Query());

        Assert.Equal(ODataQuery.DefaultTop, query.Top);
        Assert.Equal(0, query.Skip);
        Assert.Null(query.OrderBy);
        Assert.Null(query.Filter);
        Assert.Empty(query.Select);
        Assert.False(query.Count);
    }

    [Fact]
    public void Parses_top_and_skip()
    {
        var query = ODataQuery.Parse(Query(("$top", "25"), ("$skip", "50")));

        Assert.Equal(25, query.Top);
        Assert.Equal(50, query.Skip);
    }

    [Fact]
    public void Parses_orderby_and_filter_as_raw_strings()
    {
        var query = ODataQuery.Parse(Query(("$orderby", "Amount desc"), ("$filter", "Amount gt 1000")));

        Assert.Equal("Amount desc", query.OrderBy);
        Assert.Equal("Amount gt 1000", query.Filter);
    }

    [Fact]
    public void Parses_select_as_a_list_of_fields()
    {
        var query = ODataQuery.Parse(Query(("$select", "Id,DocNumber,Amount")));

        Assert.Equal(3, query.Select.Count);
        Assert.Contains("Id", query.Select);
        Assert.Contains("DocNumber", query.Select);
        Assert.Contains("Amount", query.Select);
    }

    [Fact]
    public void Count_true_when_value_is_true()
    {
        var query = ODataQuery.Parse(Query(("$count", "true")));

        Assert.True(query.Count);
    }

    [Fact]
    public void Count_false_when_value_is_not_true()
    {
        var query = ODataQuery.Parse(Query(("$count", "false")));

        Assert.False(query.Count);
    }

    [Fact]
    public void Top_above_max_is_clamped()
    {
        var query = ODataQuery.Parse(Query(("$top", "999999")));

        Assert.Equal(ODataQuery.MaxTop, query.Top);
    }

    [Fact]
    public void Negative_top_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => ODataQuery.Parse(Query(("$top", "-5"))));
    }

    [Fact]
    public void Non_numeric_top_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => ODataQuery.Parse(Query(("$top", "abc"))));
    }

    [Fact]
    public void Negative_skip_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => ODataQuery.Parse(Query(("$skip", "-1"))));
    }
}
