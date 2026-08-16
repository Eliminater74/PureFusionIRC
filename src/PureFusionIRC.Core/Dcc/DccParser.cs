using System.Globalization;
using System.Net;

namespace PureFusionIRC.Core.Dcc;

public enum DccCommandKind
{
    Send,
    Chat,
    Resume,
    Accept
}

public sealed class DccOffer
{
    public DccCommandKind Kind { get; init; }
    public string FileName { get; init; } = "file";
    public string Address { get; init; } = "0.0.0.0";
    public int Port { get; init; }
    public long FileSize { get; init; }
    public long Position { get; init; }
    public string? Token { get; init; }
    public string PeerNick { get; set; } = "";

    public bool IsReverse => Port == 0 && !string.IsNullOrEmpty(Token);
}

public static class DccParser
{
    public static bool TryParse(string payload, out DccOffer offer)
    {
        offer = null!;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        var text = payload.Trim();
        if (text.StartsWith("DCC ", StringComparison.OrdinalIgnoreCase))
        {
            text = text[4..].TrimStart();
        }

        if (!TryTakeWord(text, out var kindWord, out var rest))
        {
            return false;
        }

        if (!Enum.TryParse<DccCommandKind>(kindWord, ignoreCase: true, out var kind))
        {
            return false;
        }

        if (!TryTakeFileName(rest, out var fileName, out rest))
        {
            return false;
        }

        fileName = SafeFileName(fileName);
        var parts = SplitArgs(rest);
        if (kind is DccCommandKind.Resume or DccCommandKind.Accept)
        {
            if (parts.Count < 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
                || !long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pos))
            {
                return false;
            }

            offer = new DccOffer
            {
                Kind = kind,
                FileName = fileName,
                Port = port,
                Position = Math.Max(0, pos),
                Token = parts.Count > 2 ? parts[2] : null
            };
            return true;
        }

        if (parts.Count < 2)
        {
            return false;
        }

        if (!TryParseAddress(parts[0], out var address)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var listenPort))
        {
            return false;
        }

        long size = 0;
        string? token = null;
        if (parts.Count >= 3 && long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSize))
        {
            size = Math.Max(0, parsedSize);
            if (parts.Count >= 4)
            {
                token = parts[3];
            }
        }
        else if (parts.Count >= 3)
        {
            token = parts[2];
        }

        offer = new DccOffer
        {
            Kind = kind,
            FileName = fileName,
            Address = address,
            Port = listenPort,
            FileSize = size,
            Token = token
        };
        return true;
    }

    public static string FormatSend(string fileName, uint ipv4, int port, long size, string? token = null)
    {
        var name = fileName.Contains(' ') ? "\"" + fileName.Replace("\"", "") + "\"" : fileName;
        var line = FormattableString.Invariant($"DCC SEND {name} {ipv4} {port} {size}");
        return string.IsNullOrEmpty(token) ? line : line + " " + token;
    }

    public static string FormatResume(string fileName, int port, long position, string? token = null)
    {
        var name = fileName.Contains(' ') ? "\"" + fileName.Replace("\"", "") + "\"" : fileName;
        var line = FormattableString.Invariant($"DCC RESUME {name} {port} {position}");
        return string.IsNullOrEmpty(token) ? line : line + " " + token;
    }

    public static string FormatAccept(string fileName, int port, long position, string? token = null) =>
        FormatResume(fileName, port, position, token).Replace("RESUME", "ACCEPT", StringComparison.Ordinal);

    public static uint ToIrcIPv4(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
        {
            throw new ArgumentException("DCC SEND needs an IPv4 address.", nameof(address));
        }

        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }

    public static bool TryParseAddress(string raw, out string address)
    {
        address = "0.0.0.0";
        if (raw.Contains('.', StringComparison.Ordinal) || raw.Contains(':', StringComparison.Ordinal))
        {
            if (!IPAddress.TryParse(raw, out var parsed))
            {
                return false;
            }

            address = parsed.ToString();
            return true;
        }

        if (!ulong.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return false;
        }

        var bytes = new byte[]
        {
            (byte)((numeric >> 24) & 0xFF),
            (byte)((numeric >> 16) & 0xFF),
            (byte)((numeric >> 8) & 0xFF),
            (byte)(numeric & 0xFF)
        };
        address = new IPAddress(bytes).ToString();
        return true;
    }

    public static string SafeFileName(string name)
    {
        name = name.Replace('/', '\\');
        name = Path.GetFileName(name);
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        name = name.Trim();
        return string.IsNullOrWhiteSpace(name) ? "file.bin" : name;
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return bytes + " B";
        }

        double value = bytes;
        string[] units = ["KB", "MB", "GB", "TB"];
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return value.ToString(value >= 10 ? "0" : "0.0", CultureInfo.InvariantCulture) + " " + units[unit];
    }

    public static bool IsRiskyFile(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".exe" or ".bat" or ".cmd" or ".com" or ".scr" or ".ps1" or ".msi" or ".js" or ".vbs" or ".wsf" or ".pif" or ".reg";
    }

    private static bool TryTakeWord(string text, out string word, out string rest)
    {
        text = text.TrimStart();
        var space = text.IndexOf(' ');
        if (space < 0)
        {
            word = text;
            rest = string.Empty;
            return word.Length > 0;
        }

        word = text[..space];
        rest = text[(space + 1)..].TrimStart();
        return word.Length > 0;
    }

    private static bool TryTakeFileName(string text, out string fileName, out string rest)
    {
        text = text.TrimStart();
        if (text.StartsWith('"'))
        {
            var end = text.IndexOf('"', 1);
            if (end < 0)
            {
                fileName = text[1..];
                rest = string.Empty;
                return fileName.Length > 0;
            }

            fileName = text[1..end];
            rest = text[(end + 1)..].TrimStart();
            return fileName.Length > 0;
        }

        return TryTakeWord(text, out fileName, out rest);
    }

    private static List<string> SplitArgs(string rest) =>
        string.IsNullOrWhiteSpace(rest)
            ? []
            : rest.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
