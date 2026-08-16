using System.Text.Json;
using System.Text.Json.Serialization;
using PureFusionIRC.Core.Settings;

namespace PureFusionIRC.Core.Theming;

public sealed class ThemeUiColors
{
    public string WindowBackground { get; set; } = "#000000";
    public string PanelBackground { get; set; } = "#070B12";
    public string ChromeBackground { get; set; } = "#0A1522";
    public string ChatBackground { get; set; } = "#000000";
    public string InputBackground { get; set; } = "#0A1624";
    public string TreeBackground { get; set; } = "#05080D";
    public string NickListBackground { get; set; } = "#05080D";
    public string StatusBackground { get; set; } = "#0A1522";
    public string Border { get; set; } = "#2A5A8C";
    public string Text { get; set; } = "#F5F5F5";
    public string DimText { get; set; } = "#A8C0D8";
    public string Accent { get; set; } = "#4FC3F7";
    public string Selection { get; set; } = "#1565C0";
    public string Highlight { get; set; } = "#FF8A65";
    /// <summary>Empty values are filled at apply time so older theme JSON still loads.</summary>
    public string ButtonBackground { get; set; } = "";
    public string ButtonHover { get; set; } = "";
    public string ButtonPressed { get; set; } = "";
    public string ButtonBorder { get; set; } = "";
    public string MenuBackground { get; set; } = "";
    public string MenuHighlight { get; set; } = "";
    public string MenuHighlightText { get; set; } = "";
    public string AccentFill { get; set; } = "";
    public string AccentOn { get; set; } = "";

    public void ApplyChromeFallbacks(bool isDark)
    {
        ButtonBackground = Fallback(ButtonBackground, isDark ? "#102A43" : "#E3F2FD");
        ButtonHover = Fallback(ButtonHover, isDark ? "#1A4A73" : "#BBDEFB");
        ButtonPressed = Fallback(ButtonPressed, isDark ? "#0D3A66" : "#90CAF9");
        ButtonBorder = Fallback(ButtonBorder, isDark ? "#4FC3F7" : "#1565C0");
        MenuBackground = Fallback(MenuBackground, isDark ? "#0B1828" : "#FFFFFF");
        MenuHighlight = Fallback(MenuHighlight, isDark ? "#1565C0" : "#BBDEFB");
        MenuHighlightText = Fallback(MenuHighlightText, isDark ? "#FFFFFF" : "#0D47A1");
        AccentFill = Fallback(AccentFill, isDark ? "#1E88E5" : "#1565C0");
        AccentOn = Fallback(AccentOn, "#FFFFFF");
    }

    private static string Fallback(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
    public string SelfNick { get; set; } = "#69F0AE";
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
    public string NickOwner { get; set; } = "#E8C547";
    public string NickAdmin { get; set; } = "#EF9A9A";
    public string NickOp { get; set; } = "#FF6B6B";
    public string NickHalfop { get; set; } = "#FFB74D";
    public string NickVoice { get; set; } = "#64B5F6";
    public string NickRegular { get; set; } = "#F5F5F5";
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
        Ui = new ThemeUiColors
        {
            ButtonBackground = "#102A43",
            ButtonHover = "#1A4A73",
            ButtonPressed = "#0D3A66",
            ButtonBorder = "#4FC3F7",
            MenuBackground = "#0B1828",
            MenuHighlight = "#1565C0",
            MenuHighlightText = "#FFFFFF",
            AccentFill = "#1E88E5",
            AccentOn = "#FFFFFF"
        },
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
            ChromeBackground = "#E3F2FD",
            ChatBackground = "#FFFFFF",
            InputBackground = "#FFFFFF",
            TreeBackground = "#F5FAFF",
            NickListBackground = "#F5FAFF",
            StatusBackground = "#E3F2FD",
            Border = "#90CAF9",
            Text = "#1A1A1A",
            DimText = "#5A5A5A",
            Accent = "#1565C0",
            Selection = "#90CAF9",
            ButtonBackground = "#E3F2FD",
            ButtonHover = "#BBDEFB",
            ButtonPressed = "#90CAF9",
            ButtonBorder = "#1565C0",
            MenuBackground = "#FFFFFF",
            MenuHighlight = "#BBDEFB",
            MenuHighlightText = "#0D47A1",
            AccentFill = "#1565C0",
            AccentOn = "#FFFFFF",
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
            Mention = "#EF6C00",
            NickOwner = "#F9A825",
            NickAdmin = "#C62828",
            NickOp = "#B71C1C",
            NickHalfop = "#EF6C00",
            NickVoice = "#1565C0",
            NickRegular = "#1A1A1A"
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
            ChromeBackground = "#1A2430",
            ChatBackground = "#1E1E1E",
            InputBackground = "#1C2830",
            TreeBackground = "#1A2026",
            NickListBackground = "#1A2026",
            StatusBackground = "#1A2430",
            Border = "#3E6A80",
            Text = "#EEEEEE",
            DimText = "#B0C4CB",
            Accent = "#80CBC4",
            Selection = "#1565C0",
            ButtonBackground = "#1C3344",
            ButtonHover = "#274A62",
            ButtonPressed = "#163044",
            ButtonBorder = "#64B5F6",
            MenuBackground = "#1A2830",
            MenuHighlight = "#1565C0",
            MenuHighlightText = "#FFFFFF",
            AccentFill = "#0288D1",
            AccentOn = "#FFFFFF",
            Highlight = "#FFAB91",
            SelfNick = "#80CBC4",
            OtherNick = "#90CAF9",
            Action = "#CE93D8",
            NickOwner = "#FFD54F",
            NickAdmin = "#EF9A9A",
            NickOp = "#EF5350",
            NickHalfop = "#FFB74D",
            NickVoice = "#64B5F6",
            NickRegular = "#EEEEEE"
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
                        theme.Ui ??= new ThemeUiColors();
                        if (theme.Palette is null || theme.Palette.Length == 0)
                        {
                            theme.Palette = (string[])MircPalette.Classic.Clone();
                        }

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
            var path = PathFor(theme.Id);
            if (File.Exists(path))
            {
                continue;
            }

            File.WriteAllText(path, JsonSerializer.Serialize(theme, SettingsStore.JsonOptions));
        }
    }

    public static bool IsBuiltIn(string id) =>
        BuiltInThemes.All.Any(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    public string PathFor(string id) =>
        Path.Combine(UserThemesDirectory, id + ".json");

    public void Save(ThemeDefinition theme)
    {
        if (string.IsNullOrWhiteSpace(theme.Id))
        {
            throw new ArgumentException("Theme id is required.", nameof(theme));
        }

        Directory.CreateDirectory(UserThemesDirectory);
        theme.Ui.ApplyChromeFallbacks(theme.IsDark);
        if (theme.Palette is null || theme.Palette.Length == 0)
        {
            theme.Palette = MircPalette.Classic;
        }

        File.WriteAllText(PathFor(theme.Id), JsonSerializer.Serialize(theme, SettingsStore.JsonOptions));
    }

    public ThemeDefinition CloneAsNew(ThemeDefinition source, string name)
    {
        var copy = Clone(source);
        copy.Name = string.IsNullOrWhiteSpace(name) ? source.Name + " copy" : name.Trim();
        copy.Id = UniqueId(Slug(copy.Name));
        copy.Description = "Copy of " + source.Name;
        Save(copy);
        return copy;
    }

    public bool Delete(string id)
    {
        if (IsBuiltIn(id))
        {
            return false;
        }

        var path = PathFor(id);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    public ThemeDefinition ResetBuiltIn(string id)
    {
        var factory = BuiltInThemes.All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
        if (factory is null)
        {
            throw new InvalidOperationException("Not a built-in theme: " + id);
        }

        var fresh = Clone(factory);
        Save(fresh);
        return Get(id);
    }

    public static ThemeDefinition Clone(ThemeDefinition theme)
    {
        var copy = JsonSerializer.Deserialize<ThemeDefinition>(
            JsonSerializer.Serialize(theme, SettingsStore.JsonOptions),
            SettingsStore.JsonOptions) ?? new ThemeDefinition();
        copy.Ui ??= new ThemeUiColors();
        if (copy.Palette is null || copy.Palette.Length == 0)
        {
            copy.Palette = (string[])MircPalette.Classic.Clone();
        }
        else
        {
            copy.Palette = (string[])copy.Palette.Clone();
        }

        return copy;
    }

    public string UniqueId(string baseId)
    {
        var slug = string.IsNullOrWhiteSpace(baseId) ? "custom" : baseId;
        var ids = LoadAll().Select(t => t.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!ids.Contains(slug))
        {
            return slug;
        }

        for (var n = 2; n < 1000; n++)
        {
            var candidate = slug + "-" + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!ids.Contains(candidate))
            {
                return candidate;
            }
        }

        return slug + "-" + Guid.NewGuid().ToString("N")[..6];
    }

    public static string Slug(string name)
    {
        var chars = (name ?? "").Trim().ToLowerInvariant()
            .Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Length == 0 ? "custom" : slug;
    }
}
