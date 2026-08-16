using System.Globalization;
using System.Text;
using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Irc;
using PureFusionIRC.Core.Settings;
using PureFusionIRC.Core.Text;

namespace PureFusionIRC.Core.Logging;

/// <summary>
/// Plain-text logs under %AppData%\PureFusionIRC\logs\&lt;network&gt;\&lt;yyyy-MM-dd&gt;\&lt;buffer&gt;.log
/// </summary>
public sealed class BufferLogWriter : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, StreamWriter> _writers = new(StringComparer.OrdinalIgnoreCase);
    private readonly SettingsStore _store;

    public BufferLogWriter(SettingsStore store)
    {
        _store = store;
        Directory.CreateDirectory(store.LogsDir);
    }

    public string Root => _store.LogsDir;

    public string PathFor(IrcSession session, IrcBuffer buffer, DateTimeOffset? when = null)
    {
        var stamp = when ?? DateTimeOffset.Now;
        var network = SafeFileName(session.Network.Name);
        var folder = Path.Combine(_store.LogsDir, network, stamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return Path.Combine(folder, SafeFileName(FileStem(buffer)) + ".log");
    }

    public void Write(IrcSession session, IrcBuffer buffer, ChatLine line)
    {
        if (!session.Settings.LogBuffers)
        {
            return;
        }

        var path = PathFor(session, buffer, line.Timestamp);
        var text = FormatLine(line, session.Settings.TimestampFormat);
        lock (_gate)
        {
            try
            {
                var writer = GetWriter(path);
                writer.WriteLine(text);
            }
            catch (IOException)
            {
                // Logging must never break chat.
            }
        }
    }

    public static string FormatLine(ChatLine line, string timestampFormat)
    {
        var clock = line.Timestamp.ToString(
            string.IsNullOrWhiteSpace(timestampFormat) ? "HH:mm:ss" : timestampFormat,
            CultureInfo.InvariantCulture);
        var body = ControlCodes.Strip(line.Text ?? "");
        var nick = line.Nick ?? "";
        var core = line.Kind switch
        {
            ChatLineKind.Message when nick.Length > 0 => "<" + nick + "> " + body,
            ChatLineKind.Action when nick.Length > 0 => "* " + nick + " " + body,
            ChatLineKind.Notice when nick.Length > 0 => "-" + nick + "- " + body,
            ChatLineKind.Ctcp when nick.Length > 0 => "[" + nick + " CTCP] " + body,
            _ => body.Length == 0 ? "***" : "*** " + body
        };
        return "[" + clock + "] " + core;
    }

    public static string SafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "buffer";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var cleaned = new string(chars).Trim('.', ' ');
        if (cleaned.Length == 0)
        {
            return "buffer";
        }

        return cleaned.Length <= 80 ? cleaned : cleaned[..80];
    }

    public static string FileStem(IrcBuffer buffer) =>
        buffer.Kind == BufferKind.Server ? "server" : buffer.Name;

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var writer in _writers.Values)
            {
                writer.Dispose();
            }

            _writers.Clear();
        }

        GC.SuppressFinalize(this);
    }

    private StreamWriter GetWriter(string path)
    {
        if (_writers.TryGetValue(path, out var existing))
        {
            return existing;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8)
        {
            AutoFlush = true
        };
        _writers[path] = writer;
        return writer;
    }
}
