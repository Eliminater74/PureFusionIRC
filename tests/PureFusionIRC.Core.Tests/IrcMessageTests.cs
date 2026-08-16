using PureFusionIRC.Core.Irc;

namespace PureFusionIRC.Core.Tests;

public class IrcMessageTests
{
    [Fact]
    public void Parses_privmsg_with_prefix()
    {
        Assert.True(IrcMessage.TryParse(":nick!user@host PRIVMSG #chan :hello world", out var msg));
        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal("nick", msg.Prefix?.Nick);
        Assert.Equal("user", msg.Prefix?.User);
        Assert.Equal("host", msg.Prefix?.Host);
        Assert.Equal("#chan", msg[0]);
        Assert.Equal("hello world", msg.Trailing);
    }

    [Fact]
    public void Parses_ircv3_tags_and_escaping()
    {
        Assert.True(IrcMessage.TryParse("@time=2020-01-01T00:00:00.000Z;note=hi\\sspace :irc.example NOTICE nick :x", out var msg));
        Assert.Equal("2020-01-01T00:00:00.000Z", msg.Tags["time"]);
        Assert.Equal("hi space", msg.Tags["note"]);
        Assert.Equal("NOTICE", msg.Command);
        Assert.Equal("x", msg.Trailing);
    }

    [Fact]
    public void Parses_numeric_and_empty_trailing()
    {
        var msg = IrcMessage.MustParse(":server 001 auto :Welcome");
        Assert.Equal("001", msg.Command);
        Assert.Equal("auto", msg[0]);
        Assert.Equal("Welcome", msg.Trailing);
    }

    [Fact]
    public void Rejects_empty_line()
    {
        Assert.False(IrcMessage.TryParse("   ", out _));
        Assert.False(IrcMessage.TryParse("", out _));
    }

    [Fact]
    public void Formats_outgoing_with_trailing_colon()
    {
        var line = IrcMessage.Create("PRIVMSG", "#a", "hello there").FormatOutgoing();
        Assert.Equal("PRIVMSG #a :hello there", line);
    }
}
