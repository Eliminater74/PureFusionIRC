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
