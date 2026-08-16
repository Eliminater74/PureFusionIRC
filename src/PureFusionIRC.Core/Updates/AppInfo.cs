using System.Reflection;

namespace PureFusionIRC.Core.Updates;

public static class AppInfo
{
    public const string Product = "PureFusionIRC";
    public const string GitHubOwner = "Eliminater74";
    public const string GitHubRepo = "PureFusionIRC";

    public static string ReleasesUrl => "https://github.com/" + GitHubOwner + "/" + GitHubRepo + "/releases";
    public static string ChangelogUrl => "https://github.com/" + GitHubOwner + "/" + GitHubRepo + "/blob/main/CHANGELOG.md";

    public static string GetVersion(Assembly? assembly = null)
    {
        assembly ??= typeof(AppInfo).Assembly;
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(info))
        {
            return assembly.GetName().Version?.ToString(3) ?? "1.0.0-B2";
        }

        var plus = info.IndexOf('+');
        return plus < 0 ? info : info[..plus];
    }
}
