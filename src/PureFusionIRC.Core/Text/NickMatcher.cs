namespace PureFusionIRC.Core.Text;

/// <summary>@-mention matching against the current channel nick list.</summary>
public static class NickMatcher
{
    public static IReadOnlyList<string> Filter(IEnumerable<string> nicks, string query, int limit = 40)
    {
        query = (query ?? string.Empty).TrimStart('@');
        var unique = nicks
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IEnumerable<string> ranked;
        if (query.Length == 0)
        {
            ranked = unique.OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var starts = unique
                .Where(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
            var contains = unique
                .Where(n => n.Contains(query, StringComparison.OrdinalIgnoreCase)
                            && !n.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
            ranked = starts.Concat(contains);
        }

        return ranked.Take(limit).ToList();
    }

    public static bool TryGetAtToken(string text, int caret, out int start, out string query)
    {
        start = 0;
        query = string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        caret = Math.Clamp(caret, 0, text.Length);
        start = caret;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
        {
            start--;
        }

        var token = text[start..caret];
        if (token.Length == 0 || token[0] != '@')
        {
            return false;
        }

        query = token[1..];
        return true;
    }

    public static string InsertNick(string text, int tokenStart, int caret, string nick)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        tokenStart = Math.Clamp(tokenStart, 0, caret);
        var atLineStart = tokenStart == 0 || text[..tokenStart].All(char.IsWhiteSpace);
        var insert = atLineStart ? nick + ": " : nick + " ";
        return text[..tokenStart] + insert + text[caret..];
    }
}
