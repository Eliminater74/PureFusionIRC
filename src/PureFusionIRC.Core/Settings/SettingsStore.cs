using System.Text.Json;
using System.Text.Json.Serialization;
using PureFusionIRC.Core.Models;

namespace PureFusionIRC.Core.Settings;

public sealed class SettingsDocument
{
    public AppSettings App { get; set; } = new();
    public List<NetworkProfile> Networks { get; set; } = DefaultNetworks.Create();
}

public sealed class SettingsStore
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public SettingsStore(string? root = null)
    {
        Root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PureFusionIRC");
        SettingsPath = Path.Combine(Root, "settings.json");
        NetworksPath = Path.Combine(Root, "networks.json");
        ThemesDir = Path.Combine(Root, "themes");
        ScriptsDir = Path.Combine(Root, "scripts");
        PluginsDir = Path.Combine(Root, "plugins");
        LogsDir = Path.Combine(Root, "logs");
        BackupsDir = Path.Combine(Root, "backups");
    }

    public string Root { get; }
    public string SettingsPath { get; }
    public string NetworksPath { get; }
    public string ThemesDir { get; }
    public string ScriptsDir { get; }
    public string PluginsDir { get; }
    public string LogsDir { get; }
    public string BackupsDir { get; }

    public void EnsureLayout()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(ThemesDir);
        Directory.CreateDirectory(ScriptsDir);
        Directory.CreateDirectory(PluginsDir);
        Directory.CreateDirectory(LogsDir);
        Directory.CreateDirectory(BackupsDir);
    }

    public SettingsDocument Load()
    {
        EnsureLayout();
        var document = new SettingsDocument();
        if (File.Exists(SettingsPath))
        {
            document.App = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions) ?? new AppSettings();
        }

        if (File.Exists(NetworksPath))
        {
            document.Networks = JsonSerializer.Deserialize<List<NetworkProfile>>(File.ReadAllText(NetworksPath), JsonOptions)
                                ?? DefaultNetworks.Create();
        }

        document.App = SecretStore.UnprotectSettings(document.App);
        document.Networks = document.Networks.Select(SecretStore.UnprotectNetwork).ToList();
        var dirty = false;
        if (document.App.NetworkListRevision < DefaultNetworks.Revision)
        {
            DefaultNetworks.MergeInto(document.Networks);
            document.App.NetworkListRevision = DefaultNetworks.Revision;
            dirty = true;
        }

        if (document.App.UiRevision < 1)
        {
            document.App.MinimizeToTray = true;
            document.App.CloseToTray = false;
            document.App.TrayNotifications = true;
            document.App.UiRevision = 1;
            dirty = true;
        }

        if (dirty)
        {
            Save(document);
        }

        return document;
    }

    public void Save(SettingsDocument document)
    {
        EnsureLayout();
        var app = SecretStore.ProtectSettings(document.App);
        var networks = document.Networks.Select(SecretStore.ProtectNetwork).ToList();
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(app, JsonOptions));
        File.WriteAllText(NetworksPath, JsonSerializer.Serialize(networks, JsonOptions));
    }
}
