using PureFusionIRC.Core.Updates;

namespace PureFusionIRC.Core.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("v1.0.0-B1", 1, 0, 0, "B1")]
    [InlineData("1.0.0", 1, 0, 0, "")]
    [InlineData("1.2.3-RC2", 1, 2, 3, "RC2")]
    public void Parse_strips_v_and_splits_prerelease(string text, int major, int minor, int patch, string pre)
    {
        var version = AppVersion.Parse(text);
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(pre, version.Pre);
    }

    [Fact]
    public void Prerelease_sorts_below_the_same_numeric_release()
    {
        Assert.True(AppVersion.Parse("1.0.0-B1").CompareTo(AppVersion.Parse("1.0.0")) < 0);
        Assert.True(AppVersion.Parse("1.0.0").IsNewerThan(AppVersion.Parse("1.0.0-B1")));
    }

    [Fact]
    public void Beta_numbers_sort_numerically()
    {
        Assert.True(AppVersion.Parse("1.0.0-B2").IsNewerThan(AppVersion.Parse("1.0.0-B1")));
        Assert.True(AppVersion.Parse("1.0.0-B10").IsNewerThan(AppVersion.Parse("1.0.0-B9")));
    }

    [Fact]
    public void Patch_beats_a_previous_beta()
    {
        Assert.True(AppVersion.Parse("1.0.1-B1").IsNewerThan(AppVersion.Parse("1.0.0-B9")));
    }
}

public class GitHubUpdateClientTests
{
    [Fact]
    public void PickLatest_skips_drafts_and_needs_a_setup_exe()
    {
        var releases = new[]
        {
            Rel("v1.0.0-B2", true, false, "https://example.com/bad.exe", "nope.exe"),
            Rel("v1.0.0-B1", false, true, "https://github.com/Eliminater74/PureFusionIRC/releases/download/v1.0.0-B1/PureFusionIRC-1.0.0-B1-setup.exe")
        };

        var latest = GitHubUpdateClient.PickLatest(releases, includePrerelease: true);
        Assert.NotNull(latest);
        Assert.Equal("v1.0.0-B1", latest!.Tag);
    }

    [Fact]
    public void PickLatest_can_ignore_prereleases()
    {
        var releases = new[]
        {
            Rel("v1.0.0-B2", false, true, "https://github.com/Eliminater74/PureFusionIRC/releases/download/v1.0.0-B2/PureFusionIRC-1.0.0-B2-setup.exe"),
            Rel("v1.0.0", false, false, "https://github.com/Eliminater74/PureFusionIRC/releases/download/v1.0.0/PureFusionIRC-1.0.0-setup.exe")
        };

        var latest = GitHubUpdateClient.PickLatest(releases, includePrerelease: false);
        Assert.Equal("1.0.0", latest?.Version.ToString());
    }

    [Fact]
    public void Rejects_non_github_hosts()
    {
        Assert.False(GitHubUpdateClient.IsTrustedDownload("https://evil.example/setup.exe"));
        Assert.True(GitHubUpdateClient.IsTrustedDownload(
            "https://github.com/Eliminater74/PureFusionIRC/releases/download/v1/PureFusionIRC-1.0.0-B1-setup.exe"));
        Assert.True(GitHubUpdateClient.IsTrustedDownload(
            "https://objects.githubusercontent.com/github-production-release-asset/x"));
    }

    [Fact]
    public void ProjectStats_splits_installer_and_zip_downloads()
    {
        var repo = new GitHubRepoDto { StargazersCount = 4, ForksCount = 1, SubscribersCount = 2, OpenIssuesCount = 3 };
        var releases = new[]
        {
            Rel("v1.0.0-B3", false, false,
                "https://github.com/Eliminater74/PureFusionIRC/releases/download/v1.0.0-B3/PureFusionIRC-1.0.0-B3-setup.exe",
                downloads: 10),
            new GitHubReleaseDto
            {
                TagName = "v1.0.0-B2",
                Assets =
                [
                    new GitHubAssetDto { Name = "PureFusionIRC-1.0.0-B2-setup.exe", DownloadCount = 7 },
                    new GitHubAssetDto { Name = "PureFusionIRC-1.0.0-B2-win-x64.zip", DownloadCount = 3 }
                ]
            }
        };

        var stats = ProjectStats.From(repo, releases);
        Assert.Equal(17, stats.InstallerDownloads);
        Assert.Equal(3, stats.ZipDownloads);
        Assert.Equal(20, stats.TotalDownloads);
        Assert.Equal(2, stats.ReleaseCount);
        Assert.Equal(4, stats.Stars);
        Assert.Equal("v1.0.0-B3", stats.LatestTag);
    }

    [Fact]
    public void Embedded_changelog_is_present()
    {
        var text = ChangelogText.LoadEmbedded();
        Assert.Contains("1.0.0-B1", text, StringComparison.Ordinal);
        Assert.Contains("Added", text, StringComparison.Ordinal);
    }

    private static GitHubReleaseDto Rel(string tag, bool draft, bool pre, string url, string? name = null, long downloads = 0) => new()
    {
        TagName = tag,
        Draft = draft,
        Prerelease = pre,
        HtmlUrl = "https://github.com/Eliminater74/PureFusionIRC/releases/tag/" + tag,
        Body = "notes for " + tag,
        Assets =
        [
            new GitHubAssetDto
            {
                Name = name ?? Path.GetFileName(new Uri(url).AbsolutePath),
                BrowserDownloadUrl = url,
                Size = 123,
                DownloadCount = downloads
            }
        ]
    };
}
