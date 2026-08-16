using System.Windows;
using PureFusionIRC.Core;
using PureFusionIRC.Core.Models;
using PureFusionIRC.Core.Settings;

namespace PureFusionIRC.App.Windows;

public sealed class CountryGroup
{
    public required string Country { get; init; }
    public required List<NetworkProfile> Networks { get; init; }
}

public partial class NetworkWindow : Window
{
    private readonly ClientRuntime _runtime;
    private NetworkProfile? _current;
    public NetworkProfile? ConnectTarget { get; private set; }

    public NetworkWindow(ClientRuntime runtime)
    {
        _runtime = runtime;
        InitializeComponent();
        CountryBox.ItemsSource = DefaultNetworks.CountryOrder;
        GlobalNickBox.Text = _runtime.Document.App.Identity.Nick;
        AltNickBox.Text = _runtime.Document.App.Identity.AlternativeNick;
        UserBox.Text = _runtime.Document.App.Identity.Username;
        RealBox.Text = _runtime.Document.App.Identity.RealName;
        RebuildTree();
        SelectNetwork(_runtime.Document.Networks.FirstOrDefault(n => n.Name.StartsWith("IRCnet (USA)", StringComparison.OrdinalIgnoreCase))
                      ?? _runtime.Document.Networks.FirstOrDefault());
    }

    private void RebuildTree()
    {
        NetworkTree.ItemsSource = DefaultNetworks.GroupByCountry(_runtime.Document.Networks)
            .Select(g => new CountryGroup { Country = g.Key, Networks = g.OrderBy(n => n.Name).ToList() })
            .ToList();
    }

    private void NetworkTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is NetworkProfile profile)
        {
            Flush();
            ShowProfile(profile);
        }
    }

    private void SelectNetwork(NetworkProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        ShowProfile(profile);
    }

    private void ShowProfile(NetworkProfile profile)
    {
        _current = profile;
        var server = profile.PrimaryServer;
        CountryBox.Text = profile.Country;
        NameBox.Text = profile.Name;
        HostBox.Text = server.Host;
        PortBox.Text = server.Port.ToString();
        TlsBox.IsChecked = server.UseTls;
        BadCertBox.IsChecked = server.AcceptInvalidCertificates;
        ServerPassBox.Password = server.Password ?? "";
        NickBox.Text = profile.NickOverride ?? "";
        AutoJoinBox.Text = string.Join(", ", profile.AutoJoin);
        SaslUserBox.Text = profile.SaslAccount ?? "";
        SaslPassBox.Password = profile.SaslPassword ?? "";
        NickServBox.Password = profile.NickServPassword ?? "";
        BackupBox.Text = string.Join(Environment.NewLine, profile.Servers.Skip(1).Select(FormatServer));
        CommentBlock.Text = string.IsNullOrWhiteSpace(profile.Comment)
            ? "Identity fields apply to all networks. Passwords are stored with Windows DPAPI."
            : profile.Comment;
    }

    private static string FormatServer(ServerEntry server) =>
        server.Port is 6697 or 6667 ? server.Host + ":" + server.Port : $"{server.Host}:{server.Port}";

    private void Flush()
    {
        _runtime.Document.App.Identity.Nick = GlobalNickBox.Text.Trim();
        _runtime.Document.App.Identity.AlternativeNick = AltNickBox.Text.Trim();
        _runtime.Document.App.Identity.Username = UserBox.Text.Trim();
        _runtime.Document.App.Identity.RealName = RealBox.Text.Trim();
        if (_current is null)
        {
            return;
        }

        _current.Name = NameBox.Text.Trim();
        _current.Country = string.IsNullOrWhiteSpace(CountryBox.Text) ? "Global" : CountryBox.Text.Trim();
        var servers = new List<ServerEntry>
        {
            new()
            {
                Host = HostBox.Text.Trim(),
                Port = int.TryParse(PortBox.Text, out var port) ? port : 6697,
                UseTls = TlsBox.IsChecked == true,
                AcceptInvalidCertificates = BadCertBox.IsChecked == true,
                Password = string.IsNullOrEmpty(ServerPassBox.Password) ? null : ServerPassBox.Password
            }
        };

        foreach (var line in BackupBox.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parsed = ParseServerLine(line, TlsBox.IsChecked == true);
            if (parsed is not null)
            {
                servers.Add(parsed);
            }
        }

        _current.Servers = servers;
        _current.NickOverride = string.IsNullOrWhiteSpace(NickBox.Text) ? null : NickBox.Text.Trim();
        _current.AutoJoin = AutoJoinBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        _current.SaslAccount = string.IsNullOrWhiteSpace(SaslUserBox.Text) ? null : SaslUserBox.Text.Trim();
        _current.SaslPassword = string.IsNullOrEmpty(SaslPassBox.Password) ? null : SaslPassBox.Password;
        _current.NickServPassword = string.IsNullOrEmpty(NickServBox.Password) ? null : NickServBox.Password;
    }

    private static ServerEntry? ParseServerLine(string line, bool defaultTls)
    {
        var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts[0].Length == 0)
        {
            return null;
        }

        var port = 6697;
        if (parts.Length == 2)
        {
            int.TryParse(parts[1], out port);
        }

        return new ServerEntry
        {
            Host = parts[0],
            Port = port <= 0 ? 6697 : port,
            UseTls = defaultTls || port is 6697 or 9999 or 7000
        };
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        Flush();
        var country = _current?.Country
                      ?? (NetworkTree.SelectedItem as CountryGroup)?.Country
                      ?? "United States";
        var profile = new NetworkProfile
        {
            Name = "New network",
            Country = country,
            Servers = [new ServerEntry { Host = "irc.example.com", Port = 6697, UseTls = true }]
        };
        _runtime.AddNetwork(profile);
        RebuildTree();
        ShowProfile(profile);
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (NetworkTree.SelectedItem is not NetworkProfile profile)
        {
            return;
        }

        _runtime.RemoveNetwork(profile.Id);
        _current = null;
        RebuildTree();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Flush();
        RebuildTree();
        _runtime.Save();
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        Flush();
        _runtime.Save();
        ConnectTarget = _current ?? NetworkTree.SelectedItem as NetworkProfile;
        DialogResult = ConnectTarget is not null;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Flush();
        _runtime.Save();
        DialogResult = false;
        Close();
    }
}
