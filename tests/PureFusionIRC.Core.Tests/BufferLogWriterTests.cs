using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Logging;

namespace PureFusionIRC.Core.Tests;

public class BufferLogWriterTests
{
    [Fact]
    public void FormatLine_writes_mirc_style_chat()
    {
        var line = new ChatLine(new DateTimeOffset(2026, 8, 16, 13, 47, 12, TimeSpan.Zero),
            ChatLineKind.Message, "Eliminater", "hello \u0002world\u0002", "#c-64", isSelf: true);
        Assert.Equal("[13:47:12] <Eliminater> hello world", BufferLogWriter.FormatLine(line, "HH:mm:ss"));
    }

    [Fact]
    public void FormatLine_writes_actions_and_events()
    {
        var when = new DateTimeOffset(2026, 8, 16, 13, 47, 12, TimeSpan.Zero);
        var action = new ChatLine(when, ChatLineKind.Action, "Eliminater", "waves");
        var join = new ChatLine(when, ChatLineKind.Join, "Eliminater", "Eliminater has joined #c-64");
        Assert.Equal("[13:47:12] * Eliminater waves", BufferLogWriter.FormatLine(action, "HH:mm:ss"));
        Assert.Equal("[13:47:12] *** Eliminater has joined #c-64", BufferLogWriter.FormatLine(join, "HH:mm:ss"));
    }

    [Fact]
    public void SafeFileName_keeps_hash_and_strips_illegal_chars()
    {
        Assert.Equal("#c-64", BufferLogWriter.SafeFileName("#c-64"));
        Assert.Equal("IRCnet_ USA_", BufferLogWriter.SafeFileName("IRCnet: USA?"));
        Assert.Equal("buffer", BufferLogWriter.SafeFileName("   "));
    }
}
