namespace PureFusionIRC.Core.Text;

/// <summary>mIRC / HexChat-style attribute codes used in PRIVMSG bodies.</summary>
public static class ControlCodes
{
    public const char Bold = '\u0002';
    public const char Color = '\u0003';
    public const char HexColor = '\u0004';
    public const char Reset = '\u000F';
    public const char Reverse = '\u0016';
    public const char Italic = '\u001D';
    public const char Strikethrough = '\u001E';
    public const char Underline = '\u001F';

    public static string Strip(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var chars = new char[text.Length];
        var n = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is Bold or Reset or Reverse or Italic or Strikethrough or Underline)
            {
                continue;
            }

            if (c == Color)
            {
                i = SkipMircColor(text, i);
                continue;
            }

            if (c == HexColor)
            {
                i = SkipHexColor(text, i);
                continue;
            }

            chars[n++] = c;
        }

        return new string(chars, 0, n);
    }

    public static IReadOnlyList<TextSpan> Parse(string text)
    {
        var spans = new List<TextSpan>();
        var style = TextStyle.Plain;
        var start = 0;

        void Flush(int end)
        {
            if (end > start)
            {
                spans.Add(new TextSpan(text[start..end], style));
            }
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case Bold:
                    Flush(i);
                    style = style with { Bold = !style.Bold };
                    start = i + 1;
                    break;
                case Italic:
                    Flush(i);
                    style = style with { Italic = !style.Italic };
                    start = i + 1;
                    break;
                case Underline:
                    Flush(i);
                    style = style with { Underline = !style.Underline };
                    start = i + 1;
                    break;
                case Strikethrough:
                    Flush(i);
                    style = style with { Strikethrough = !style.Strikethrough };
                    start = i + 1;
                    break;
                case Reverse:
                    Flush(i);
                    style = style with { Reverse = !style.Reverse };
                    start = i + 1;
                    break;
                case Reset:
                    Flush(i);
                    style = TextStyle.Plain;
                    start = i + 1;
                    break;
                case Color:
                    Flush(i);
                    var fg = ReadNumber(text, i + 1, 2, out var afterFg);
                    int? bg = null;
                    var next = afterFg;
                    if (next < text.Length && text[next] == ',')
                    {
                        bg = ReadNumber(text, next + 1, 2, out next);
                    }

                    style = style with { Foreground = fg, Background = bg };
                    start = next;
                    i = next - 1;
                    break;
                case HexColor:
                    Flush(i);
                    start = SkipHexColor(text, i) + 1;
                    i = start - 1;
                    break;
            }
        }

        Flush(text.Length);
        return spans;
    }

    private static int SkipMircColor(string text, int index)
    {
        var i = index + 1;
        i = SkipDigits(text, i, 2);
        if (i < text.Length && text[i] == ',')
        {
            i = SkipDigits(text, i + 1, 2);
        }

        return i - 1;
    }

    private static int SkipHexColor(string text, int index)
    {
        var i = index + 1;
        i = SkipHex(text, i, 6);
        if (i < text.Length && text[i] == ',')
        {
            i = SkipHex(text, i + 1, 6);
        }

        return i - 1;
    }

    private static int? ReadNumber(string text, int index, int maxDigits, out int next)
    {
        next = index;
        if (index >= text.Length || !char.IsDigit(text[index]))
        {
            return null;
        }

        var len = 0;
        while (len < maxDigits && index + len < text.Length && char.IsDigit(text[index + len]))
        {
            len++;
        }

        next = index + len;
        return int.Parse(text.AsSpan(index, len));
    }

    private static int SkipDigits(string text, int index, int max)
    {
        var n = 0;
        while (n < max && index < text.Length && char.IsDigit(text[index]))
        {
            index++;
            n++;
        }

        return index;
    }

    private static int SkipHex(string text, int index, int max)
    {
        var n = 0;
        while (n < max && index < text.Length && Uri.IsHexDigit(text[index]))
        {
            index++;
            n++;
        }

        return index;
    }
}

public readonly record struct TextStyle(
    bool Bold = false,
    bool Italic = false,
    bool Underline = false,
    bool Strikethrough = false,
    bool Reverse = false,
    int? Foreground = null,
    int? Background = null)
{
    public static TextStyle Plain => default;
}

public sealed record TextSpan(string Text, TextStyle Style);
