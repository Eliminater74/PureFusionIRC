using System.Windows;
using System.Windows.Media;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.App.Theming;

public static class ThemeApplication
{
    public static void Apply(ThemeDefinition theme, ResourceDictionary resources)
    {
        Set(resources, "WindowBackgroundBrush", theme.Ui.WindowBackground);
        Set(resources, "PanelBackgroundBrush", theme.Ui.PanelBackground);
        Set(resources, "ChromeBackgroundBrush", theme.Ui.ChromeBackground);
        Set(resources, "ChatBackgroundBrush", theme.Ui.ChatBackground);
        Set(resources, "InputBackgroundBrush", theme.Ui.InputBackground);
        Set(resources, "TreeBackgroundBrush", theme.Ui.TreeBackground);
        Set(resources, "NickListBackgroundBrush", theme.Ui.NickListBackground);
        Set(resources, "StatusBackgroundBrush", theme.Ui.StatusBackground);
        Set(resources, "BorderBrushKey", theme.Ui.Border);
        Set(resources, "PrimaryTextBrush", theme.Ui.Text);
        Set(resources, "DimTextBrush", theme.Ui.DimText);
        Set(resources, "AccentBrush", theme.Ui.Accent);
        Set(resources, "SelectionBrush", theme.Ui.Selection);
        Set(resources, "HighlightBrush", theme.Ui.Highlight);
        Set(resources, "SelfNickBrush", theme.Ui.SelfNick);
        Set(resources, "OtherNickBrush", theme.Ui.OtherNick);
        Set(resources, "ActionBrush", theme.Ui.Action);
        Set(resources, "LinkBrush", theme.Ui.Link);
        Set(resources, "ErrorBrush", theme.Ui.Error);
        Set(resources, "JoinBrush", theme.Ui.Join);
        Set(resources, "PartBrush", theme.Ui.Part);
        Set(resources, "ChannelBrush", theme.Ui.Channel);
        Set(resources, "QueryBrush", theme.Ui.Query);
        Set(resources, "UnreadBrush", theme.Ui.Unread);
        Set(resources, "MentionBrush", theme.Ui.Mention);
        Set(resources, "NickOwnerBrush", theme.Ui.NickOwner);
        Set(resources, "NickAdminBrush", theme.Ui.NickAdmin);
        Set(resources, "NickOpBrush", theme.Ui.NickOp);
        Set(resources, "NickHalfopBrush", theme.Ui.NickHalfop);
        Set(resources, "NickVoiceBrush", theme.Ui.NickVoice);
        Set(resources, "NickRegularBrush", theme.Ui.NickRegular);

        resources["ThemeFontFamily"] = new FontFamily("Consolas");
        resources["IsDarkTheme"] = theme.IsDark;
        resources["CurrentTheme"] = theme;
    }

    public static Color Parse(string hex) =>
        (Color)ColorConverter.ConvertFromString(hex);

    public static SolidColorBrush PaletteBrush(ThemeDefinition theme, int? index)
    {
        if (index is null)
        {
            return Brushes.Transparent;
        }

        var palette = theme.Palette;
        var i = Math.Clamp(index.Value, 0, Math.Max(0, palette.Length - 1));
        return new SolidColorBrush(Parse(palette[i]));
    }

    private static void Set(ResourceDictionary resources, string key, string hex)
    {
        var brush = new SolidColorBrush(Parse(hex));
        brush.Freeze();
        resources[key] = brush;
    }
}
