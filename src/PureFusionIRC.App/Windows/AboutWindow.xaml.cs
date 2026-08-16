using System.Diagnostics;
using System.Windows;
using PureFusionIRC.Core.Updates;

namespace PureFusionIRC.App.Windows;

public partial class AboutWindow : Window
{
    private readonly GitHubUpdateClient _client;
    private readonly string _version;

    public AboutWindow(GitHubUpdateClient client, string version)
    {
        _client = client;
        _version = version;
        InitializeComponent();
        TitleBlock.Text = "PureFusionIRC " + version;
        Loaded += async (_, _) => await LoadStatsAsync().ConfigureAwait(true);
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            var stats = await _client.GetStatsAsync().ConfigureAwait(true);
            StatsBlock.Text =
                "This build:            " + _version + Environment.NewLine +
                "Latest GitHub tag:     " + (stats.LatestTag ?? "(none)") + Environment.NewLine +
                "Installer downloads:   " + stats.InstallerDownloads.ToString("N0") + Environment.NewLine +
                "Portable zip:          " + stats.ZipDownloads.ToString("N0") + Environment.NewLine +
                "All release files:     " + stats.TotalDownloads.ToString("N0") + Environment.NewLine +
                "Public releases:       " + stats.ReleaseCount.ToString("N0") + Environment.NewLine +
                "Stars / forks / watch: " + stats.Stars.ToString("N0") + " / "
                    + stats.Forks.ToString("N0") + " / " + stats.Watchers.ToString("N0") + Environment.NewLine +
                "Open issues:           " + stats.OpenIssues.ToString("N0");
        }
        catch (Exception ex)
        {
            StatsBlock.Text = "Could not reach GitHub (" + ex.Message + "). Open the Releases page for download counts.";
        }
    }

    private void GitHub_Click(object sender, RoutedEventArgs e) =>
        Open("https://github.com/" + AppInfo.GitHubOwner + "/" + AppInfo.GitHubRepo);

    private void Releases_Click(object sender, RoutedEventArgs e) => Open(AppInfo.ReleasesUrl);

    private static void Open(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
}
