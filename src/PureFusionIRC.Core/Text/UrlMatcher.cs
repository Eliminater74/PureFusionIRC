using System.Text.RegularExpressions;

namespace PureFusionIRC.Core.Text;

public readonly record struct UrlMatch(int Index, int Length, string Display, Uri Navigate);

/// <summary>Finds http(s) and www. URLs in chat text for clickable inlines.</summary>
public static class UrlMatcher
{
    private static readonly Regex Pattern = new(
        @"\b((?:https?://|www\.)[^\s<>""'\\]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static IEnumerable<UrlMatch> Find(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        foreach (Match match in Pattern.Matches(text))
        {
            var raw = match.Value;
            var trimmed = raw.TrimEnd('.', ',', ';', ':', ')', ']', '}', '!', '\'', '"');
            if (trimmed.Length < 4)
            {
                continue;
            }

            var href = trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? "https://" + trimmed
                : trimmed;
            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https"))
            {
                continue;
            }

            yield return new UrlMatch(match.Index, trimmed.Length, trimmed, uri);
        }
    }

    public static void Open(Uri uri)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            // No default browser, or shell association missing.
        }
    }
}
