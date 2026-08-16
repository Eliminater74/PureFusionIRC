using System.Windows;
using PureFusionIRC.Core;
using PureFusionIRC.Core.Models;

namespace PureFusionIRC.App.Windows;

public partial class NetworkWindow : Window
{
    private readonly ClientRuntime _runtime;
    private NetworkProfile? _current;
    public NetworkProfile? ConnectTarget { get; private set; }

    public NetworkWindow(ClientRuntime runtime)
    {
        _runtime = runtime;
        InitializeComponent();
        NetworkList.ItemsSource = _runtime.Document.Networks;
        GlobalNickBox.Text = _runtime.Document.App.Identity.Nick;
        AltNickBox.Text = _runtime.Document.App.Identity.AlternativeNick;
        UserBox.Text = _runtime.Document.App.Identity.Username;
        RealBox.Text = _runtime.Document.App.Identity.RealName;
        if (_runtime.Document.Networks.Count > 0)
        {
            NetworkList.SelectedIndex = 0;
        }
    }

    private void NetworkList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        Flush();
        _current = NetworkList.SelectedItem as NetworkProfile;
        if (_current is null)
        {
            return;
        }

        var server = _current.PrimaryServer;
        NameBox.Text = _current.Name;
        HostBox.Text = server.Host;
        PortBox.Text = server.Port.ToString();
        TlsBox.IsChecked = server.UseTls;
        BadCertBox.IsChecked = server.AcceptInvalidCertificates;
        ServerPassBox.Password = server.Password ?? "";
        NickBox.Text = _current.NickOverride ?? "";
        AutoJoinBox.Text = string.Join(", ", _current.AutoJoin);
        SaslUserBox.Text = _current.SaslAccount ?? "";
        SaslPassBox.Password = _current.SaslPassword ?? "";
        NickServBox.Password = _current.NickServPassword ?? "";
    }

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
        if (_current.Servers.Count == 0)
        {
            _current.Servers.Add(new ServerEntry());
        }

        var server = _current.Servers[0];
        server.Host = HostBox.Text.Trim();
        server.Port = int.TryParse(PortBox.Text, out var port) ? port : 6697;
        server.UseTls = TlsBox.IsChecked == true;
        server.AcceptInvalidCertificates = BadCertBox.IsChecked == true;
        server.Password = string.IsNullOrEmpty(ServerPassBox.Password) ? null : ServerPassBox.Password;
        _current.NickOverride = string.IsNullOrWhiteSpace(NickBox.Text) ? null : NickBox.Text.Trim();
        _current.AutoJoin = AutoJoinBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        _current.SaslAccount = string.IsNullOrWhiteSpace(SaslUserBox.Text) ? null : SaslUserBox.Text.Trim();
        _current.SaslPassword = string.IsNullOrEmpty(SaslPassBox.Password) ? null : SaslPassBox.Password;
        _current.NickServPassword = string.IsNullOrEmpty(NickServBox.Password) ? null : NickServBox.Password;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        Flush();
        var profile = new NetworkProfile
        {
            Name = "New network",
            Servers = [new ServerEntry { Host = "irc.example.com", Port = 6697, UseTls = true }]
        };
        _runtime.AddNetwork(profile);
        NetworkList.Items.Refresh();
        NetworkList.SelectedItem = profile;
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (NetworkList.SelectedItem is not NetworkProfile profile)
        {
            return;
        }

        _runtime.RemoveNetwork(profile.Id);
        _current = null;
        NetworkList.Items.Refresh();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Flush();
        NetworkList.Items.Refresh();
        _runtime.Save();
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        Flush();
        _runtime.Save();
        ConnectTarget = _current ?? NetworkList.SelectedItem as NetworkProfile;
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
