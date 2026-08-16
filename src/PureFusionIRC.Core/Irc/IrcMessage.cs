using System.Globalization;
using System.Text;

namespace PureFusionIRC.Core.Irc;

/// <summary>
/// One IRC protocol line: optional IRCv3 tags, prefix, command, parameters.
/// Parser follows RFC 1459/2812 plus IRCv3 message-tags escaping.
/// </summary>
public sealed class IrcMessage
{
    public IrcMessage(
        IReadOnlyDictionary<string, string?> tags,
        IrcPrefix? prefix,
        string command,
        IReadOnlyList<string> parameters)
    {
        Tags = tags;
        Prefix = prefix;
        Command = command;
        Parameters = parameters;
    }

    public IReadOnlyDictionary<string, string?> Tags { get; }
    public IrcPrefix? Prefix { get; }
    public string Command { get; }
    public IReadOnlyList<string> Parameters { get; }

    public string? Trailing => Parameters.Count == 0 ? null : Parameters[^1];

    public string? this[int index] =>
        index >= 0 && index < Parameters.Count ? Parameters[index] : null;

    public static bool TryParse(string line, out IrcMessage message)
    {
        message = null!;
        if (string.IsNullOrEmpty(line))
        {
            return false;
        }

        if (line.EndsWith("\r\n", StringComparison.Ordinal))
        {
            line = line[..^2];
        }
        else if (line.EndsWith('\n') || line.EndsWith('\r'))
        {
            line = line[..^1];
        }

        if (line.Length == 0)
        {
            return false;
        }

        var tags = (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>(StringComparer.Ordinal);
        var pos = 0;

        if (line[0] == '@')
        {
            var space = line.IndexOf(' ');
            if (space < 0)
            {
                return false;
            }

            tags = ParseTags(line[1..space]);
            pos = space + 1;
            while (pos < line.Length && line[pos] == ' ')
            {
                pos++;
            }
        }

        IrcPrefix? prefix = null;
        if (pos < line.Length && line[pos] == ':')
        {
            var space = line.IndexOf(' ', pos);
            if (space < 0)
            {
                return false;
            }

            prefix = IrcPrefix.Parse(line[(pos + 1)..space]);
            pos = space + 1;
            while (pos < line.Length && line[pos] == ' ')
            {
                pos++;
            }
        }

        if (pos >= line.Length)
        {
            return false;
        }

        var commandEnd = line.IndexOf(' ', pos);
        string command;
        var parameters = new List<string>();
        if (commandEnd < 0)
        {
            command = line[pos..];
        }
        else
        {
            command = line[pos..commandEnd];
            pos = commandEnd + 1;
            while (pos < line.Length && line[pos] == ' ')
            {
                pos++;
            }

            while (pos < line.Length)
            {
                if (line[pos] == ':')
                {
                    parameters.Add(line[(pos + 1)..]);
                    break;
                }

                var next = line.IndexOf(' ', pos);
                if (next < 0)
                {
                    parameters.Add(line[pos..]);
                    break;
                }

                parameters.Add(line[pos..next]);
                pos = next + 1;
                while (pos < line.Length && line[pos] == ' ')
                {
                    pos++;
                }
            }
        }

        if (string.IsNullOrEmpty(command))
        {
            return false;
        }

        message = new IrcMessage(tags, prefix, command, parameters);
        return true;
    }

    public static IrcMessage MustParse(string line)
    {
        if (!TryParse(line, out var message))
        {
            throw new FormatException("Not a valid IRC line: " + line);
        }

        return message;
    }

    public string FormatOutgoing()
    {
        var builder = new StringBuilder();
        if (Tags.Count > 0)
        {
            builder.Append('@');
            var first = true;
            foreach (var pair in Tags)
            {
                if (!first)
                {
                    builder.Append(';');
                }

                first = false;
                builder.Append(pair.Key);
                if (pair.Value is not null)
                {
                    builder.Append('=');
                    builder.Append(EscapeTagValue(pair.Value));
                }
            }

            builder.Append(' ');
        }

        if (Prefix is not null)
        {
            builder.Append(':');
            builder.Append(Prefix.Raw);
            builder.Append(' ');
        }

        builder.Append(Command);
        for (var i = 0; i < Parameters.Count; i++)
        {
            builder.Append(' ');
            var value = Parameters[i];
            var last = i == Parameters.Count - 1;
            if (last && (value.Length == 0 || value.Contains(' ') || value.StartsWith(':')))
            {
                builder.Append(':');
            }

            builder.Append(value);
        }

        return builder.ToString();
    }

    public static IrcMessage Create(string command, params string[] parameters) =>
        new(new Dictionary<string, string?>(), null, command, parameters);

    private static IReadOnlyDictionary<string, string?> ParseTags(string raw)
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (raw.Length == 0)
        {
            return map;
        }

        foreach (var piece in raw.Split(';'))
        {
            if (piece.Length == 0)
            {
                continue;
            }

            var eq = piece.IndexOf('=');
            if (eq < 0)
            {
                map[piece] = null;
            }
            else
            {
                map[piece[..eq]] = UnescapeTagValue(piece[(eq + 1)..]);
            }
        }

        return map;
    }

    private static string UnescapeTagValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                builder.Append(value[i + 1] switch
                {
                    ':' => ';',
                    's' => ' ',
                    '\\' => '\\',
                    'r' => '\r',
                    'n' => '\n',
                    var other => other
                });
                i++;
            }
            else
            {
                builder.Append(value[i]);
            }
        }

        return builder.ToString();
    }

    private static string EscapeTagValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case ';':
                    builder.Append(@"\:");
                    break;
                case ' ':
                    builder.Append(@"\s");
                    break;
                case '\\':
                    builder.Append(@"\\");
                    break;
                case '\r':
                    builder.Append(@"\r");
                    break;
                case '\n':
                    builder.Append(@"\n");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }

    public DateTimeOffset Timestamp
    {
        get
        {
            if (Tags.TryGetValue("time", out var time) &&
                time is not null &&
                DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }

            return DateTimeOffset.Now;
        }
    }
}
