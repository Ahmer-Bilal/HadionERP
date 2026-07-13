namespace Platform.Api.Tests;

/// <summary>
/// Proves <see cref="PagedResult{T}"/> correctly slices a full set with $top/$skip while reporting the
/// full count. This is the envelope every List Report endpoint returns — getting the paging math wrong
/// would break every list view, so it's pinned directly.
/// </summary>
public class PagedResultTests
{
    private static IReadOnlyList<int> Range(int count) => Enumerable.Range(1, count).ToList();

    [Fact]
    public void From_applies_top_and_skip()
    {
        var full = Range(100);

        var result = PagedResult<int>.From(full, skip: 10, top: 5);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(11, result.Items[0]); // skipped first 10, so first is 11
        Assert.Equal(15, result.Items[^1]);
        Assert.Equal(100, result.TotalCount);
    }

    [Fact]
    public void From_with_top_zero_returns_all_remaining_after_skip()
    {
        var full = Range(10);

        var result = PagedResult<int>.From(full, skip: 3, top: 0);

        Assert.Equal(7, result.Items.Count);
        Assert.Equal(4, result.Items[0]);
        Assert.Equal(10, result.TotalCount);
    }

    [Fact]
    public void From_skip_beyond_end_returns_empty_but_total_stays_full()
    {
        var full = Range(5);

        var result = PagedResult<int>.From(full, skip: 100, top: 10);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public void From_preserves_skip_and_top_in_the_envelope()
    {
        var result = PagedResult<int>.From(Range(50), skip: 5, top: 10);

        Assert.Equal(5, result.Skip);
        Assert.Equal(10, result.Top);
    }
}
