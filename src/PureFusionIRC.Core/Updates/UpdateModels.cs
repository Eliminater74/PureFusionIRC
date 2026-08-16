using System.Text.Json.Serialization;

namespace PureFusionIRC.Core.Updates;

public sealed class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAssetDto> Assets { get; set; } = [];
}

public sealed class GitHubAssetDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("download_count")]
    public long DownloadCount { get; set; }
}

public sealed class UpdateOffer
{
    public required AppVersion Version { get; init; }
    public required string Tag { get; init; }
    public required string Title { get; init; }
    public required string Notes { get; init; }
    public required string HtmlUrl { get; init; }
    public required string InstallerName { get; init; }
    public required string InstallerUrl { get; init; }
    public long InstallerSize { get; init; }
    public bool Prerelease { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }

    public bool IsNewerThan(AppVersion current) => Version.IsNewerThan(current);
}

public sealed class GitHubRepoDto
{
    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; set; }

    [JsonPropertyName("forks_count")]
    public int ForksCount { get; set; }

    [JsonPropertyName("open_issues_count")]
    public int OpenIssuesCount { get; set; }

    [JsonPropertyName("subscribers_count")]
    public int SubscribersCount { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";
}

public sealed class ProjectStats
{
    public long InstallerDownloads { get; init; }
    public long ZipDownloads { get; init; }
    public long OtherDownloads { get; init; }
    public int ReleaseCount { get; init; }
    public int Stars { get; init; }
    public int Forks { get; init; }
    public int Watchers { get; init; }
    public int OpenIssues { get; init; }
    public string? LatestTag { get; init; }

    public long TotalDownloads => InstallerDownloads + ZipDownloads + OtherDownloads;

    public static ProjectStats From(GitHubRepoDto? repo, IEnumerable<GitHubReleaseDto>? releases)
    {
        long setup = 0, zip = 0, other = 0;
        var count = 0;
        string? latest = null;
        if (releases is not null)
        {
            foreach (var release in releases)
            {
                if (release.Draft)
                {
                    continue;
                }

                count++;
                if (latest is null)
                {
                    latest = release.TagName;
                }

                foreach (var asset in release.Assets)
                {
                    var name = asset.Name;
                    if (name.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        setup += asset.DownloadCount;
                    }
                    else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        zip += asset.DownloadCount;
                    }
                    else
                    {
                        other += asset.DownloadCount;
                    }
                }
            }
        }

        return new ProjectStats
        {
            InstallerDownloads = setup,
            ZipDownloads = zip,
            OtherDownloads = other,
            ReleaseCount = count,
            Stars = repo?.StargazersCount ?? 0,
            Forks = repo?.ForksCount ?? 0,
            Watchers = repo?.SubscribersCount ?? 0,
            OpenIssues = repo?.OpenIssuesCount ?? 0,
            LatestTag = latest
        };
    }
}
