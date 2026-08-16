using PureFusionIRC.Core.Dcc;

namespace PureFusionIRC.Core.Tests;

public sealed class DccParserTests
{
    [Fact]
    public void Parse_classic_send_converts_integer_ip()
    {
        Assert.True(DccParser.TryParse(@"DCC SEND photo.jpg 2130706433 5000 1234", out var offer));
        Assert.Equal(DccCommandKind.Send, offer.Kind);
        Assert.Equal("photo.jpg", offer.FileName);
        Assert.Equal("127.0.0.1", offer.Address);
        Assert.Equal(5000, offer.Port);
        Assert.Equal(1234, offer.FileSize);
        Assert.False(offer.IsReverse);
    }

    [Fact]
    public void Parse_reverse_send_with_quoted_name_and_token()
    {
        Assert.True(DccParser.TryParse(@"SEND ""my file.txt"" 2130706433 0 99 tok42", out var offer));
        Assert.Equal("my file.txt", offer.FileName);
        Assert.True(offer.IsReverse);
        Assert.Equal("tok42", offer.Token);
        Assert.Equal(99, offer.FileSize);
    }

    [Fact]
    public void Format_and_ipv4_roundtrip()
    {
        var ip = System.Net.IPAddress.Parse("203.0.113.5");
        var num = DccParser.ToIrcIPv4(ip);
        var line = DccParser.FormatSend("hello world.bin", num, 0, 50, "aa");
        Assert.True(DccParser.TryParse(line, out var offer));
        Assert.Equal("hello world.bin", offer.FileName);
        Assert.Equal("203.0.113.5", offer.Address);
        Assert.True(offer.IsReverse);
        Assert.Equal("20 B", DccParser.FormatBytes(20));
        Assert.True(DccParser.IsRiskyFile("Setup.EXE"));
        Assert.False(DccParser.IsRiskyFile("notes.txt"));
    }
}
