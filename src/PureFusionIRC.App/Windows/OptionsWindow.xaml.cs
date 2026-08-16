using System.Globalization;
using System.Windows;
using Microsoft.Win32;
using PureFusionIRC.Core;

namespace PureFusionIRC.App.Windows;

public partial class OptionsWindow : Window
{
    private readonly ClientRuntime _runtime;

    public OptionsWindow(ClientRuntime runtime)
    {
        _runtime = runtime;
        InitializeComponent();
        var app = runtime.Document.App;
        var id = app.Identity;
        TimestampsBox.IsChecked = app.ShowTimestamps;
        ReconnectBox.IsChecked = app.Reconnect;
        HideJoinBox.IsChecked = app.HideJoinPart;
        StripColorBox.IsChecked = app.StripColors;
        LogBox.IsChecked = app.LogBuffers;
        MotdBox.IsChecked = app.ShowMotd;
        MinimizeTrayBox.IsChecked = app.MinimizeToTray;
        CloseTrayBox.IsChecked = app.CloseToTray;
        NotifyTrayBox.IsChecked = app.TrayNotifications;
        DccBox.IsChecked = app.DccEnabled;
        DccReverseBox.IsChecked = app.DccPreferReverse;
        DccFolderBox.Text = app.DccDownloadFolder;
        TimestampBox.Text = app.TimestampFormat;
        FontBox.Text = app.FontFamily;
        FontSizeBox.Text = app.FontSize.ToString(CultureInfo.InvariantCulture);
        HighlightBox.Text = string.Join(", ", app.HighlightWords);
        NickBox.Text = id.Nick;
        AltBox.Text = id.AlternativeNick;
        UserBox.Text = id.Username;
        RealBox.Text = id.RealName;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var app = _runtime.Document.App;
        app.ShowTimestamps = TimestampsBox.IsChecked == true;
        app.Reconnect = ReconnectBox.IsChecked == true;
        app.HideJoinPart = HideJoinBox.IsChecked == true;
        app.StripColors = StripColorBox.IsChecked == true;
        app.LogBuffers = LogBox.IsChecked == true;
        app.ShowMotd = MotdBox.IsChecked == true;
        app.MinimizeToTray = MinimizeTrayBox.IsChecked == true;
        app.CloseToTray = CloseTrayBox.IsChecked == true;
        app.TrayNotifications = NotifyTrayBox.IsChecked == true;
        app.DccEnabled = DccBox.IsChecked == true;
        app.DccPreferReverse = DccReverseBox.IsChecked == true;
        app.DccDownloadFolder = DccFolderBox.Text.Trim();
        app.TimestampFormat = string.IsNullOrWhiteSpace(TimestampBox.Text) ? "HH:mm:ss" : TimestampBox.Text.Trim();
        app.FontFamily = string.IsNullOrWhiteSpace(FontBox.Text) ? "Consolas" : FontBox.Text.Trim();
        if (double.TryParse(FontSizeBox.Text, CultureInfo.InvariantCulture, out var size) && size >= 8 && size <= 36)
        {
            app.FontSize = size;
        }

        app.HighlightWords = HighlightBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        app.Identity.Nick = NickBox.Text.Trim();
        app.Identity.AlternativeNick = AltBox.Text.Trim();
        app.Identity.Username = UserBox.Text.Trim();
        app.Identity.RealName = RealBox.Text.Trim();
        _runtime.Save();
        DialogResult = true;
        Close();
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Save incoming files to",
            Multiselect = false
        };
        if (!string.IsNullOrWhiteSpace(DccFolderBox.Text))
        {
            dialog.InitialDirectory = DccFolderBox.Text.Trim();
        }

        if (dialog.ShowDialog(this) == true)
        {
            DccFolderBox.Text = dialog.FolderName;
        }
    }
}
