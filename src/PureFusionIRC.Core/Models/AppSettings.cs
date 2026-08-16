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
    public bool ConnectOnStartup { get; set; }
    public bool Enabled { get; set; } = true;

    [JsonIgnore]
    public ServerEntry PrimaryServer =>
        Servers.Count > 0 ? Servers[0] : new ServerEntry();
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
}
