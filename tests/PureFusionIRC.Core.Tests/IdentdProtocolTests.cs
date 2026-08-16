using PureFusionIRC.Core.Ident;

namespace PureFusionIRC.Core.Tests;

public class IdentdProtocolTests
{
    [Theory]
    [InlineData("113, 6667", 113, 6667)]
    [InlineData("  54321 , 6697 \r\n", 54321, 6697)]
    public void Parses_rfc1413_queries(string line, int localPort, int remotePort)
    {
        Assert.True(IdentdProtocol.TryParseQuery(line, out var local, out var remote));
        Assert.Equal(localPort, local);
        Assert.Equal(remotePort, remote);
    }

    [Fact]
    public void Rejects_junk()
    {
        Assert.False(IdentdProtocol.TryParseQuery("hello", out _, out _));
        Assert.False(IdentdProtocol.TryParseQuery("0, 6667", out _, out _));
    }

    [Fact]
    public void Formats_userid_and_strips_username()
    {
        Assert.Equal("1234, 6667 : USERID : UNIX : purefusion",
            IdentdProtocol.FormatUserId(1234, 6667, "pure fusion!"));
        Assert.Equal("user", IdentdProtocol.SanitizeUser("!!!"));
    }

    [Fact]
    public async Task Server_answers_on_loopback()
    {
        using var server = new IdentdServer(() => "PureUser");
        var error = server.Start(System.Net.IPAddress.Loopback, 0);
        Assert.Null(error);
        Assert.True(server.Port is > 0 and <= 65535);

        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(System.Net.IPAddress.Loopback, server.Port);
        await using var stream = client.GetStream();
        using var writer = new StreamWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
        using var reader = new StreamReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        await writer.WriteLineAsync("45000, 6697");
        var reply = await reader.ReadLineAsync();
        Assert.Equal("45000, 6697 : USERID : UNIX : PureUser", reply);
    }
}
