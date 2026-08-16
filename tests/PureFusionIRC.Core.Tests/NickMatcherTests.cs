using PureFusionIRC.Core.Text;

namespace PureFusionIRC.Core.Tests;

public sealed class NickMatcherTests
{
    [Fact]
    public void Filter_prefers_prefix_then_contains()
    {
        var hits = NickMatcher.Filter(["Alice", "Mike", "Mia", "Sam"], "mi");
        Assert.Equal(new[] { "Mia", "Mike" }, hits);
    }

    [Fact]
    public void TryGetAtToken_reads_query_at_caret()
    {
        Assert.True(NickMatcher.TryGetAtToken("hi @mi", 6, out var start, out var query));
        Assert.Equal(3, start);
        Assert.Equal("mi", query);
        Assert.False(NickMatcher.TryGetAtToken("hello mi", 8, out _, out _));
    }

    [Fact]
    public void InsertNick_uses_colon_at_line_start()
    {
        Assert.Equal("Mike: ", NickMatcher.InsertNick("@mi", 0, 3, "Mike"));
        Assert.Equal("hey Mike ", NickMatcher.InsertNick("hey @m", 4, 6, "Mike"));
    }
}
