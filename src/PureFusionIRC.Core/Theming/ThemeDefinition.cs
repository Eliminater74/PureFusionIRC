using System.Text.Json;
using System.Text.Json.Serialization;
using PureFusionIRC.Core.Settings;

namespace PureFusionIRC.Core.Theming;

public sealed class ThemeUiColors
{
    public string WindowBackground { get; set; } = "#000000";
    public string PanelBackground { get; set; } = "#0A0A0A";
    public string ChromeBackground { get; set; } = "#111111";
    public string ChatBackground { get; set; } = "#000000";
    public string InputBackground { get; set; } = "#0D0D0D";
    public string TreeBackground { get; set; } = "#050505";
    public string NickListBackground { get; set; } = "#050505";
    public string StatusBackground { get; set; } = "#111111";
    public string Border { get; set; } = "#2A2A2A";
    public string Text { get; set; } = "#F5F5F5";
    public string DimText { get; set; } = "#A0A0A0";
    public string Accent { get; set; } = "#4FC3F7";
    public string Selection { get; set; } = "#1565C0";
    public string Highlight { get; set; } = "#FF8A65";
    public string SelfNick { get; set; } = "#80CBC4";
    public string OtherNick { get; set; } = "#90CAF9";
    public string Action { get; set; } = "#CE93D8";
    public string Link { get; set; } = "#64B5F6";
    public string Error { get; set; } = "#EF9A9A";
    public string Join { get; set; } = "#A5D6A7";
    public string Part { get; set; } = "#FFAB91";
    public string Channel { get; set; } = "#FFE082";
    public string Query { get; set; } = "#81D4FA";
    public string Unread { get; set; } = "#81C784";
    public string Mention { get; set; } = "#FFCC80";
}

public sealed class ThemeDefinition
{
    public string Id { get; set; } = "amoled-black";
    public string Name { get; set; } = "AMOLED Black";
    public string Description { get; set; } = "True black chrome and chat with white text.";
    public bool IsDark { get; set; } = true;
    public ThemeUiColors Ui { get; set; } = new();
    public string[] Palette { get; set; } = MircPalette.Classic;
}

public static class MircPalette
{
    // Standard 99-color mIRC/HexChat palette. First 16 are the classic codes.
    public static readonly string[] Classic =
    [
        "#CCCCCC", "#000000", "#3636B2", "#2A8C2A", "#C33B3B", "#C73232", "#802080", "#D2691E",
        "#D8D84A", "#3DCE3D", "#19580E", "#2E8C8C", "#4545E6", "#B037B0", "#4C4C4C", "#959595",
        "#E0E0E0", "#3A3A3A", "#00005F", "#000087", "#0000AF", "#0000D7", "#0000FF", "#005F00",
        "#005F5F", "#005F87", "#005FAF", "#005FD7", "#005FFF", "#008700", "#00875F", "#008787",
        "#0087AF", "#0087D7", "#0087FF", "#00AF00", "#00AF5F", "#00AF87", "#00AFAF", "#00AFD7",
        "#00AFFF", "#00D700", "#00D75F", "#00D787", "#00D7AF", "#00D7D7", "#00D7FF", "#00FF00",
        "#00FF5F", "#00FF87", "#00FFAF", "#00FFD7", "#00FFFF", "#5F0000", "#5F005F", "#5F0087",
        "#5F00AF", "#5F00D7", "#5F00FF", "#5F5F00", "#5F5F5F", "#5F5F87", "#5F5FAF", "#5F5FD7",
        "#5F5FFF", "#5F8700", "#5F875F", "#5F8787", "#5F87AF", "#5F87D7", "#5F87FF", "#5FAF00",
        "#5FAF5F", "#5FAF87", "#5FAFAF", "#5FAFD7", "#5FAFFF", "#5FD700", "#5FD75F", "#5FD787",
        "#5FD7AF", "#5FD7D7", "#5FD7FF", "#5FFF00", "#5FFF5F", "#5FFF87", "#5FFFAF", "#5FFFD7",
        "#5FFFFF", "#870000", "#87005F", "#870087", "#8700AF", "#8700D7", "#8700FF", "#875F00",
        "#875F5F", "#875F87", "#875FAF"
    ];
}

public static class BuiltInThemes
{
    public static ThemeDefinition AmoledBlack { get; } = new()
    {
        Id = "amoled-black",
        Name = "AMOLED Black",
        Description = "Pure black (#000000) with white text. Default OLED theme.",
        IsDark = true,
        Ui = new ThemeUiColors(),
        Palette = MircPalette.Classic
    };

    public static ThemeDefinition ClassicLight { get; } = new()
    {
        Id = "classic-light",
        Name = "Classic Light",
        Description = "mIRC-inspired light panels, dark text, pale gray chrome.",
        IsDark = false,
        Ui = new ThemeUiColors
        {
            WindowBackground = "#F0F0F0",
            PanelBackground = "#FFFFFF",
            ChromeBackground = "#E8E8E8",
            ChatBackground = "#FFFFFF",
            InputBackground = "#FFFFFF",
            TreeBackground = "#F7F7F7",
            NickListBackground = "#F7F7F7",
            StatusBackground = "#E8E8E8",
            Border = "#B0B0B0",
            Text = "#1A1A1A",
            DimText = "#5A5A5A",
            Accent = "#1565C0",
            Selection = "#BBDEFB",
            Highlight = "#E65100",
            SelfNick = "#00695C",
            OtherNick = "#0D47A1",
            Action = "#6A1B9A",
            Link = "#1565C0",
            Error = "#B71C1C",
            Join = "#2E7D32",
            Part = "#C62828",
            Channel = "#F9A825",
            Query = "#0277BD",
            Unread = "#2E7D32",
            Mention = "#EF6C00"
        },
        Palette = MircPalette.Classic
    };

    public static ThemeDefinition Charcoal { get; } = new()
    {
        Id = "charcoal",
        Name = "Charcoal",
        Description = "Softer dark gray than AMOLED; easier on bright rooms.",
        IsDark = true,
        Ui = new ThemeUiColors
        {
            WindowBackground = "#1B1B1B",
            PanelBackground = "#242424",
            ChromeBackground = "#2A2A2A",
            ChatBackground = "#1E1E1E",
            InputBackground = "#2A2A2A",
            TreeBackground = "#202020",
            NickListBackground = "#202020",
            StatusBackground = "#2A2A2A",
            Border = "#3C3C3C",
            Text = "#EEEEEE",
            DimText = "#B0B0B0",
            Accent = "#80CBC4",
            Selection = "#37474F",
            Highlight = "#FFAB91",
            SelfNick = "#80CBC4",
            OtherNick = "#90CAF9",
            Action = "#CE93D8"
        },
        Palette = MircPalette.Classic
    };

    public static IReadOnlyList<ThemeDefinition> All { get; } = [AmoledBlack, ClassicLight, Charcoal];
}

public sealed class ThemeCatalog
{
    public ThemeCatalog(string userThemesDirectory)
    {
        UserThemesDirectory = userThemesDirectory;
    }

    public string UserThemesDirectory { get; }

    public IReadOnlyList<ThemeDefinition> LoadAll()
    {
        var map = BuiltInThemes.All.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(UserThemesDirectory))
        {
            foreach (var file in Directory.GetFiles(UserThemesDirectory, "*.json"))
            {
                try
                {
                    var theme = JsonSerializer.Deserialize<ThemeDefinition>(File.ReadAllText(file), SettingsStore.JsonOptions);
                    if (theme is not null && !string.IsNullOrWhiteSpace(theme.Id))
                    {
                        map[theme.Id] = theme;
                    }
                }
                catch (JsonException)
                {
                    // User theme files can be half-written; skip rather than crash the client.
                }
            }
        }

        return map.Values.OrderBy(t => t.Name).ToList();
    }

    public ThemeDefinition Get(string? id)
    {
        var all = LoadAll();
        return all.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase))
               ?? BuiltInThemes.AmoledBlack;
    }

    public void SeedUserCopies()
    {
        Directory.CreateDirectory(UserThemesDirectory);
        foreach (var theme in BuiltInThemes.All)
        {
            var path = Path.Combine(UserThemesDirectory, theme.Id + ".json");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonSerializer.Serialize(theme, SettingsStore.JsonOptions));
            }
        }
    }
}
