using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using PureFusionIRC.App.Theming;
using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Models;
using PureFusionIRC.Core.Text;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.App.Chat;

public static class ChatDocumentBuilder
{

    public static Paragraph Build(ChatLine line, ThemeDefinition theme, AppSettings settings)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 1, 0, 1), Tag = line };
        if (line.IsHighlight)
        {
            paragraph.Background = Brush("MentionBrush", theme.Ui.Mention);
        }

        if (settings.ShowTimestamps)
        {
            paragraph.Inlines.Add(Run(line.Timestamp.ToString(settings.TimestampFormat) + " ", Dim(theme)));
        }

        switch (line.Kind)
        {
            case ChatLineKind.Action:
                paragraph.Inlines.Add(Run("* ", Brush("ActionBrush", theme.Ui.Action)));
                paragraph.Inlines.Add(Run((line.Nick ?? "") + " ", Brush("SelfNickBrush", line.IsSelf ? theme.Ui.SelfNick : theme.Ui.OtherNick)));
                AddBody(paragraph, line.Text, theme, settings, Brush("ActionBrush", theme.Ui.Action));
                break;
            case ChatLineKind.Message:
            case ChatLineKind.Notice:
                var nickColor = line.IsSelf ? theme.Ui.SelfNick : theme.Ui.OtherNick;
                var label = line.Kind == ChatLineKind.Notice ? "-" + line.Nick + "-" : "<" + line.Nick + ">";
                paragraph.Inlines.Add(Run(label + " ", Brush("OtherNickBrush", nickColor)));
                AddBody(paragraph, line.Text, theme, settings, Brush("PrimaryTextBrush", theme.Ui.Text));
                break;
            default:
                var color = line.Kind switch
                {
                    ChatLineKind.Error => theme.Ui.Error,
                    ChatLineKind.Join => theme.Ui.Join,
                    ChatLineKind.Part or ChatLineKind.Quit or ChatLineKind.Kick => theme.Ui.Part,
                    ChatLineKind.Topic or ChatLineKind.Mode => theme.Ui.Channel,
                    _ => theme.Ui.DimText
                };
                paragraph.Inlines.Add(Run("*** " + (line.Nick is null ? "" : line.Nick + " "), Dim(theme)));
                AddBody(paragraph, line.Text, theme, settings, Brush("DimTextBrush", color));
                break;
        }

        return paragraph;
    }

    private static void AddBody(Paragraph paragraph, string text, ThemeDefinition theme, AppSettings settings, SolidColorBrush fallback)
    {
        if (settings.StripColors)
        {
            AddPlainWithLinks(paragraph, ControlCodes.Strip(text), fallback, theme);
            return;
        }

        foreach (var span in ControlCodes.Parse(text))
        {
            var brush = span.Style.Foreground is int fg
                ? ThemeApplication.PaletteBrush(theme, fg)
                : fallback;
            if (span.Style.Reverse)
            {
                brush = Brush("ChatBackgroundBrush", theme.Ui.ChatBackground);
            }

            AddPlainWithLinks(paragraph, span.Text, brush, theme, span.Style);
        }
    }

    private static void AddPlainWithLinks(Paragraph paragraph, string text, SolidColorBrush brush, ThemeDefinition theme, TextStyle? style = null)
    {
        var last = 0;
        foreach (var match in UrlMatcher.Find(text))
        {
            if (match.Index > last)
            {
                paragraph.Inlines.Add(Styled(text[last..match.Index], brush, style));
            }

            var display = new Run(match.Display);
            var link = new Hyperlink(display)
            {
                Cursor = Cursors.Hand,
                Foreground = Brush("LinkBrush", theme.Ui.Link),
                TextDecorations = TextDecorations.Underline,
                ToolTip = match.Navigate.AbsoluteUri,
                Tag = match.Navigate
            };
            // Do not set NavigateUri — that can route into a Frame. Always shell-open the system browser.
            link.Click += OpenInDefaultBrowser;
            paragraph.Inlines.Add(link);
            last = match.Index + match.Length;
        }

        if (last < text.Length)
        {
            paragraph.Inlines.Add(Styled(text[last..], brush, style));
        }
    }

    private static void OpenInDefaultBrowser(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Hyperlink { Tag: Uri uri })
        {
            UrlMatcher.Open(uri);
        }
    }

    private static Run Styled(string text, SolidColorBrush brush, TextStyle? style)
    {
        var run = Run(text, brush);
        if (style is { } s)
        {
            if (s.Bold)
            {
                run.FontWeight = FontWeights.Bold;
            }

            if (s.Italic)
            {
                run.FontStyle = FontStyles.Italic;
            }

            if (s.Underline)
            {
                run.TextDecorations = TextDecorations.Underline;
            }

            if (s.Strikethrough)
            {
                run.TextDecorations = TextDecorations.Strikethrough;
            }
        }

        return run;
    }

    private static Run Run(string text, Brush brush) => new(text) { Foreground = brush };

    private static SolidColorBrush Dim(ThemeDefinition theme) => Brush("DimTextBrush", theme.Ui.DimText);

    private static SolidColorBrush Brush(string key, string hex)
    {
        if (Application.Current?.TryFindResource(key) is SolidColorBrush existing)
        {
            return existing;
        }

        var brush = new SolidColorBrush(ThemeApplication.Parse(hex));
        brush.Freeze();
        return brush;
    }
}
