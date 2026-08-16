using System.Net.Http.Headers;
using System.Text.Json;

namespace PureFusionIRC.Core.Updates;

public sealed class GitHubUpdateClient
{
    public const long MaxInstallerBytes = 200L * 1024 * 1024;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly string _releasesPath;
    private readonly string _repoPath;

    public GitHubUpdateClient(HttpClient http, string owner = AppInfo.GitHubOwner, string repo = AppInfo.GitHubRepo)
    {
        _http = http;
        _releasesPath = "repos/" + owner + "/" + repo + "/releases?per_page=100";
        _repoPath = "repos/" + owner + "/" + repo;
        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(AppInfo.Product + "/" + AppInfo.GetVersion());
        }

        if (_http.DefaultRequestHeaders.Accept.Count == 0)
        {
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }
    }

    public static HttpClient CreateHttp()
    {
        return new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<UpdateOffer?> GetLatestAsync(bool includePrerelease, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(_releasesPath, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var releases = await JsonSerializer.DeserializeAsync<List<GitHubReleaseDto>>(stream, Json, cancellationToken)
            .ConfigureAwait(false);
        return PickLatest(releases, includePrerelease);
    }

    public async Task<ProjectStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var repoTask = _http.GetAsync(_repoPath, cancellationToken);
        var relTask = _http.GetAsync(_releasesPath, cancellationToken);
        await Task.WhenAll(repoTask, relTask).ConfigureAwait(false);

        using var repoResponse = await repoTask.ConfigureAwait(false);
        using var relResponse = await relTask.ConfigureAwait(false);
        repoResponse.EnsureSuccessStatusCode();
        relResponse.EnsureSuccessStatusCode();

        await using var repoStream = await repoResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var relStream = await relResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var repo = await JsonSerializer.DeserializeAsync<GitHubRepoDto>(repoStream, Json, cancellationToken)
            .ConfigureAwait(false);
        var releases = await JsonSerializer.DeserializeAsync<List<GitHubReleaseDto>>(relStream, Json, cancellationToken)
            .ConfigureAwait(false);
        return ProjectStats.From(repo, releases);
    }

    public static UpdateOffer? PickLatest(IEnumerable<GitHubReleaseDto>? releases, bool includePrerelease)
    {
        if (releases is null)
        {
            return null;
        }

        UpdateOffer? best = null;
        foreach (var release in releases)
        {
            if (!TryCreateOffer(release, includePrerelease, out var offer) || offer is null)
            {
                continue;
            }

            if (best is null
                || offer.Version.CompareTo(best.Version) > 0
                || (offer.Version.CompareTo(best.Version) == 0 && offer.PublishedAt > best.PublishedAt))
            {
                best = offer;
            }
        }

        return best;
    }

    public static bool TryCreateOffer(GitHubReleaseDto release, bool includePrerelease, out UpdateOffer? offer)
    {
        offer = null;
        if (release.Draft || (release.Prerelease && !includePrerelease))
        {
            return false;
        }

        if (!AppVersion.TryParse(release.TagName, out var version))
        {
            return false;
        }

        var asset = FindInstaller(release.Assets);
        if (asset is null || !IsTrustedDownload(asset.BrowserDownloadUrl))
        {
            return false;
        }

        offer = new UpdateOffer
        {
            Version = version,
            Tag = release.TagName,
            Title = string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name!,
            Notes = string.IsNullOrWhiteSpace(release.Body) ? "(No release notes.)" : release.Body.Trim(),
            HtmlUrl = string.IsNullOrWhiteSpace(release.HtmlUrl) ? AppInfo.ReleasesUrl : release.HtmlUrl,
            InstallerName = asset.Name,
            InstallerUrl = asset.BrowserDownloadUrl,
            InstallerSize = asset.Size,
            Prerelease = release.Prerelease,
            PublishedAt = release.PublishedAt
        };
        return true;
    }

    public static GitHubAssetDto? FindInstaller(IEnumerable<GitHubAssetDto>? assets)
    {
        if (assets is null)
        {
            return null;
        }

        return assets.FirstOrDefault(a =>
            a.Name.StartsWith("PureFusionIRC-", StringComparison.OrdinalIgnoreCase)
            && a.Name.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsTrustedDownload(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.Host;
        return host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
               || host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task DownloadInstallerAsync(
        UpdateOffer offer,
        string destinationPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        if (!IsTrustedDownload(offer.InstallerUrl))
        {
            throw new InvalidOperationException("Installer URL is not a GitHub download.");
        }

        if (offer.InstallerSize > MaxInstallerBytes)
        {
            throw new InvalidOperationException("Installer is larger than expected.");
        }

        using var response = await _http.GetAsync(offer.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? offer.InstallerSize;
        if (total > MaxInstallerBytes)
        {
            throw new InvalidOperationException("Installer is larger than expected.");
        }

        var folder = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        var buffer = new byte[81920];
        long copied = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            copied += read;
            if (copied > MaxInstallerBytes)
            {
                throw new InvalidOperationException("Installer is larger than expected.");
            }

            if (total > 0)
            {
                progress?.Report(Math.Clamp((double)copied / total, 0, 1));
            }
        }

        progress?.Report(1);
    }
}
