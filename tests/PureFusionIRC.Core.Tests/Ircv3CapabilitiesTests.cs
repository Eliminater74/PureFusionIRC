using PureFusionIRC.Core.Irc;

namespace PureFusionIRC.Core.Tests;

public class Ircv3CapabilitiesTests
{
    [Fact]
    public void Multiline_LS_waits_for_the_final_line_before_REQ()
    {
        var caps = new Ircv3Capabilities();
        var first = IrcMessage.MustParse(":irc CAP * LS * :multi-prefix sasl=PLAIN,EXTERNAL echo-message");
        Assert.Empty(caps.NoteLs(first, wantSasl: true));

        var last = IrcMessage.MustParse(":irc CAP * LS :account-notify chathistory");
        var want = caps.NoteLs(last, wantSasl: true);
        Assert.Contains("multi-prefix", want);
        Assert.Contains("echo-message", want);
        Assert.Contains("account-notify", want);
        Assert.Contains("chathistory", want);
        Assert.Contains("sasl", want);
    }

    [Fact]
    public void ACK_marks_enabled_caps()
    {
        var caps = new Ircv3Capabilities();
        var ack = IrcMessage.MustParse(":irc CAP * ACK :echo-message server-time");
        Assert.True(caps.NoteAck(ack));
        Assert.True(caps.Has("echo-message"));
        Assert.True(caps.Has("server-time"));
        Assert.False(caps.Has("sasl"));
    }

    [Fact]
    public void ChannelTarget_uses_param_zero_for_extended_join()
    {
        var join = IrcMessage.MustParse(":nick!u@h JOIN #c-64 * :real name here");
        Assert.Equal("#c-64", Ircv3Capabilities.ChannelTarget(join));
        Assert.Equal("real name here", join.Trailing);
    }

    [Fact]
    public void BareNick_strips_userhost_in_names()
    {
        Assert.Equal("Eliminater", Ircv3Capabilities.BareNick("Eliminater!user@host"));
        Assert.Equal("Eliminater", Ircv3Capabilities.BareNick("Eliminater"));
    }

    [Fact]
    public void Formats_outgoing_reply_tag()
    {
        var tags = new Dictionary<string, string?> { ["+draft/reply"] = "abc-msgid" };
        var line = new IrcMessage(tags, null, "PRIVMSG", ["#c-64", "got it"]).FormatOutgoing();
        Assert.Equal("@+draft/reply=abc-msgid PRIVMSG #c-64 :got it", line);
    }
}
