using System.Reflection;

namespace PureFusionIRC.Core.Updates;

public static class ChangelogText
{
    public const string ResourceName = "PureFusionIRC.CHANGELOG.md";

    public static string LoadEmbedded(Assembly? assembly = null)
    {
        assembly ??= typeof(ChangelogText).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return "Changelog is not bundled in this build. See " + AppInfo.ChangelogUrl;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
