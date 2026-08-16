using System.Text.Json.Serialization;

namespace PureFusionIRC.Core.Models;

public sealed class UserIdentity
{
    public string Nick { get; set; } = "PureUser";
    public string AlternativeNick { get; set; } = "PureUser_";
    public string Username { get; set; } = "purefusion";
    public string RealName { get; set; } = "PureFusionIRC";
    public bool Invisible { get; set; } = true;
}

public sealed class ServerEntry
{
    public string Host { get; set; } = "irc.libera.chat";
    public int Port { get; set; } = 6697;
    public bool UseTls { get; set; } = true;
    public bool AcceptInvalidCertificates { get; set; }
    public string? Password { get; set; }
}

public sealed class NetworkProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Libera Chat";
    public List<ServerEntry> Servers { get; set; } = new();
    public List<string> AutoJoin { get; set; } = new();
    public string? NickOverride { get; set; }
    public string? SaslAccount { get; set; }
    public string? SaslPassword { get; set; }
    public string? NickServPassword { get; set; }
    public string Country { get; set; } = "Global";
    public string? Comment { get; set; }
    public bool ConnectOnStartup { get; set; }
    public bool Enabled { get; set; } = true;

    [JsonIgnore]
    public ServerEntry PrimaryServer =>
        Servers.Count > 0 ? Servers[0] : new ServerEntry();

    public bool HasAutoJoin(string channel)
    {
        var token = NormalizeAutoJoinName(channel);
        return token.Length > 0 &&
               AutoJoin.Any(entry => string.Equals(NormalizeAutoJoinName(entry), token, StringComparison.OrdinalIgnoreCase));
    }

    public bool SetAutoJoin(string channel, bool enabled)
    {
        var token = NormalizeAutoJoinName(channel);
        if (token.Length == 0)
        {
            return false;
        }

        if (enabled)
        {
            if (!HasAutoJoin(token))
            {
                AutoJoin.Add(token);
            }

            return true;
        }

        AutoJoin.RemoveAll(entry =>
            string.Equals(NormalizeAutoJoinName(entry), token, StringComparison.OrdinalIgnoreCase));
        return false;
    }

    /// <summary>
    /// Channels to JOIN after reconnect: rooms still open, plus any saved auto-join (keys preserved).
    /// </summary>
    public IReadOnlyList<string> JoinTargets(IEnumerable<string> currentlyJoined)
    {
        var result = new List<string>();
        foreach (var name in currentlyJoined)
        {
            AddJoinSpec(result, SpecForChannel(name));
        }

        foreach (var entry in AutoJoin)
        {
            AddJoinSpec(result, entry);
        }

        return result;
    }

    private string SpecForChannel(string channel)
    {
        var token = NormalizeAutoJoinName(channel);
        var saved = AutoJoin.FirstOrDefault(entry =>
            string.Equals(NormalizeAutoJoinName(entry), token, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(saved) ? token : saved;
    }

    private static void AddJoinSpec(List<string> result, string spec)
    {
        var token = NormalizeAutoJoinName(spec);
        if (token.Length == 0)
        {
            return;
        }

        if (result.Any(existing =>
                string.Equals(NormalizeAutoJoinName(existing), token, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        result.Add(spec.Trim());
    }

    public static string AutoJoinChannelName(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return string.Empty;
        }

        return entry.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
    }

    public static string NormalizeAutoJoinName(string entry)
    {
        var token = AutoJoinChannelName(entry);
        if (token.Length == 0)
        {
            return string.Empty;
        }

        return token[0] is '#' or '&' or '+' or '!' ? token : "#" + token;
    }

    /// <summary>Turns "c-64" or "#c-64 key" into a JOIN target the server will accept.</summary>
    public static string NormalizeJoinSpec(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return string.Empty;
        }

        var parts = spec.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var name = NormalizeAutoJoinName(parts[0]);
        if (name.Length == 0)
        {
            return string.Empty;
        }

        return parts.Length > 1 ? name + " " + parts[1] : name;
    }

    public static List<string> ParseAutoJoinList(IEnumerable<string> entries)
    {
        var result = new List<string>();
        foreach (var entry in entries)
        {
            AddJoinSpec(result, NormalizeJoinSpec(entry));
        }

        return result;
    }
}

public sealed class AppSettings
{
    public UserIdentity Identity { get; set; } = new();
    public string ThemeId { get; set; } = "amoled-black";
    public bool ShowTree { get; set; } = true;
    public bool ShowNickList { get; set; } = true;
    public bool ShowToolbar { get; set; } = true;
    public bool ShowTimestamps { get; set; } = true;
    public string TimestampFormat { get; set; } = "HH:mm:ss";
    public string FontFamily { get; set; } = "Consolas";
    public double FontSize { get; set; } = 13;
    public bool Reconnect { get; set; } = true;
    public int ReconnectDelaySeconds { get; set; } = 8;
    public bool HideJoinPart { get; set; }
    public bool StripColors { get; set; }
    public bool FlashOnHighlight { get; set; } = true;
    public bool LogBuffers { get; set; } = true;
    public int MaxBufferLines { get; set; } = 5000;
    public List<string> HighlightWords { get; set; } = new();
    public bool ShowMotd { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; }
    public bool TrayNotifications { get; set; } = true;
    public bool DccEnabled { get; set; } = true;
    public bool DccPreferReverse { get; set; } = true;
    public string DccDownloadFolder { get; set; } = "";
    public bool CheckForUpdates { get; set; } = true;
    public bool IncludePrereleaseUpdates { get; set; } = true;
    /// <summary>Bumped for one-time UI defaults (tray). Do not reuse NetworkListRevision.</summary>
    public int UiRevision { get; set; }
    /// <summary>Bumped when built-in country server lists change so AppData picks up new entries.</summary>
    public int NetworkListRevision { get; set; }
}
