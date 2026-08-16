using PureFusionIRC.Core.Text;

namespace PureFusionIRC.Core.Tests;

public sealed class UrlMatcherTests
{
    [Fact]
    public void Find_http_and_www_and_strips_trailing_punctuation()
    {
        var hits = UrlMatcher.Find("see https://example.com/path, and www.test.org) please").ToList();
        Assert.Equal(2, hits.Count);
        Assert.Equal("https://example.com/path", hits[0].Display);
        Assert.Equal("https://example.com/path", hits[0].Navigate.AbsoluteUri.TrimEnd('/'));
        Assert.Equal("www.test.org", hits[1].Display);
        Assert.Equal("https://www.test.org/", hits[1].Navigate.AbsoluteUri);
    }

    [Fact]
    public void Find_ignores_plain_words()
    {
        Assert.Empty(UrlMatcher.Find("no links in #channel or user@host"));
    }
}
