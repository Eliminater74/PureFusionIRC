using System.Diagnostics;
using System.IO;
using System.Windows;
using PureFusionIRC.Core.Updates;

namespace PureFusionIRC.App.Windows;

public partial class UpdateWindow : Window
{
    private readonly GitHubUpdateClient _client;
    private readonly bool _includePrerelease;
    private UpdateOffer? _offer;
    private CancellationTokenSource? _downloadCts;
    private bool _busy;

    public UpdateWindow(GitHubUpdateClient client, string currentVersion, bool includePrerelease, UpdateOffer? offer = null)
    {
        _client = client;
        _includePrerelease = includePrerelease;
        CurrentVersion = currentVersion;
        InitializeComponent();
        InstallButton.IsEnabled = false;
        if (offer is not null)
        {
            ShowOffer(offer);
        }

        Loaded += async (_, _) => await CheckAsync().ConfigureAwait(true);
    }

    public string CurrentVersion { get; }

    private async Task CheckAsync()
    {
        if (_offer is not null)
        {
            return;
        }

        StatusBlock.Text = "Checking GitHub Releases…";
        MetaBlock.Text = "You are on " + CurrentVersion;
        NotesBox.Text = "";
        try
        {
            var latest = await _client.GetLatestAsync(_includePrerelease).ConfigureAwait(true);
            if (latest is null)
            {
                StatusBlock.Text = "No installer release was found yet.";
                NotesBox.Text = "When a GitHub Release is published, this window can download PureFusionIRC-*-setup.exe.";
                return;
            }

            if (AppVersion.TryParse(CurrentVersion, out var current) && !latest.IsNewerThan(current))
            {
                StatusBlock.Text = "You are up to date.";
                MetaBlock.Text = "Installed " + CurrentVersion + "  ·  Latest " + latest.Version;
                NotesBox.Text = latest.Notes;
                return;
            }

            ShowOffer(latest);
        }
        catch (Exception ex)
        {
            StatusBlock.Text = "Could not check for updates.";
            NotesBox.Text = ex.Message;
        }
    }

    public void ShowOffer(UpdateOffer offer)
    {
        _offer = offer;
        StatusBlock.Text = "Update available: " + offer.Version;
        MetaBlock.Text = "Installed " + CurrentVersion + "  ·  " + offer.InstallerName
            + (offer.Prerelease ? "  ·  beta" : "");
        NotesBox.Text = offer.Notes;
        InstallButton.IsEnabled = true;
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (_offer is null || _busy)
        {
            return;
        }

        _busy = true;
        InstallButton.IsEnabled = false;
        Progress.Visibility = Visibility.Visible;
        Progress.Value = 0;
        _downloadCts = new CancellationTokenSource();
        var dest = Path.Combine(Path.GetTempPath(), "PureFusionIRC-update-" + _offer.Version + "-setup.exe");
        try
        {
            var progress = new Progress<double>(value => Progress.Value = value);
            await _client.DownloadInstallerAsync(_offer, dest, progress, _downloadCts.Token).ConfigureAwait(true);
            StatusBlock.Text = "Starting the installer… PureFusionIRC will close, then come back on the new version.";
            Process.Start(new ProcessStartInfo
            {
                FileName = dest,
                Arguments = "/SILENT /CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS /NORESTART /SUPPRESSMSGBOXES",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusBlock.Text = "Download failed.";
            NotesBox.Text = ex.Message + Environment.NewLine + Environment.NewLine + NotesBox.Text;
            InstallButton.IsEnabled = true;
        }
        finally
        {
            _busy = false;
        }
    }

    private void Web_Click(object sender, RoutedEventArgs e)
    {
        var url = _offer?.HtmlUrl ?? AppInfo.ReleasesUrl;
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    protected override void OnClosed(EventArgs e)
    {
        _downloadCts?.Cancel();
        base.OnClosed(e);
    }
}
