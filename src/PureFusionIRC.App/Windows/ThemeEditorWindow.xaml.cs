using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PureFusionIRC.App.Theming;
using PureFusionIRC.Core;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.App.Windows;

public partial class ThemeEditorWindow : Window
{
    private static readonly (string Property, string Group, string Label)[] ColorFields =
    [
        ("WindowBackground", "Surfaces", "Window"),
        ("PanelBackground", "Surfaces", "Panels"),
        ("ChromeBackground", "Surfaces", "Menus / toolbar"),
        ("ChatBackground", "Surfaces", "Chat"),
        ("InputBackground", "Surfaces", "Input box"),
        ("TreeBackground", "Surfaces", "Server tree"),
        ("NickListBackground", "Surfaces", "Nick list"),
        ("StatusBackground", "Surfaces", "Status bar"),
        ("Border", "Surfaces", "Borders"),
        ("Text", "Text", "Primary text"),
        ("DimText", "Text", "Dim text / timestamps"),
        ("Accent", "Text", "Accent"),
        ("Selection", "Text", "Selection"),
        ("Mention", "Text", "Mention bar in chat"),
        ("Highlight", "Text", "Highlight accent"),
        ("Link", "Text", "Links"),
        ("SelfNick", "Chat", "Your nick"),
        ("OtherNick", "Chat", "Other nicks"),
        ("Action", "Chat", "/me actions"),
        ("Error", "Chat", "Errors"),
        ("Join", "Chat", "Joins"),
        ("Part", "Chat", "Parts / quits / kicks"),
        ("Channel", "Chat", "Channel names / topic"),
        ("Query", "Chat", "Queries"),
        ("Unread", "Chat", "Unread buffers"),
        ("NickOwner", "Nick ranks", "Owner (~)"),
        ("NickAdmin", "Nick ranks", "Admin (&)"),
        ("NickOp", "Nick ranks", "Op (@)"),
        ("NickHalfop", "Nick ranks", "Half-op (%)"),
        ("NickVoice", "Nick ranks", "Voice (+)"),
        ("NickRegular", "Nick ranks", "Regular"),
        ("ButtonBackground", "Buttons and menus", "Button"),
        ("ButtonHover", "Buttons and menus", "Button hover"),
        ("ButtonPressed", "Buttons and menus", "Button pressed"),
        ("ButtonBorder", "Buttons and menus", "Button border"),
        ("MenuBackground", "Buttons and menus", "Menu"),
        ("MenuHighlight", "Buttons and menus", "Menu highlight"),
        ("MenuHighlightText", "Buttons and menus", "Menu highlight text"),
        ("AccentFill", "Buttons and menus", "Accent fill"),
        ("AccentOn", "Buttons and menus", "Text on accent")
    ];

    private readonly ClientRuntime _runtime;
    private readonly string _startedThemeId;
    private ThemeDefinition? _working;
    private bool _loading;
    private bool _dirty;

    public ThemeEditorWindow(ClientRuntime runtime)
    {
        _runtime = runtime;
        _startedThemeId = runtime.Theme.Id;
        InitializeComponent();
        Closing += ThemeEditor_Closing;
        BuildFields();
        ReloadList(runtime.Theme.Id);
    }

    private void ReloadList(string? selectId)
    {
        _loading = true;
        var all = _runtime.Themes.LoadAll();
        ThemeList.ItemsSource = all;
        ThemeList.SelectedItem = all.FirstOrDefault(t => string.Equals(t.Id, selectId, StringComparison.OrdinalIgnoreCase))
            ?? all.FirstOrDefault();
        _loading = false;
        LoadSelected();
    }

    private void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (_dirty && _working is not null)
        {
            var answer = MessageBox.Show(this,
                "Save changes to " + _working.Name + "?",
                "Theme editor",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel)
            {
                _loading = true;
                ThemeList.SelectedItem = e.RemovedItems.OfType<ThemeDefinition>().FirstOrDefault()
                    ?? ThemeList.SelectedItem;
                _loading = false;
                return;
            }

            if (answer == MessageBoxResult.Yes)
            {
                _runtime.SaveTheme(_working);
            }
        }

        LoadSelected();
    }

    private void LoadSelected()
    {
        if (ThemeList.SelectedItem is not ThemeDefinition listed)
        {
            return;
        }

        _working = ThemeCatalog.Clone(_runtime.Themes.Get(listed.Id));
        _dirty = false;
        _loading = true;
        NameBox.Text = _working.Name;
        DescBox.Text = _working.Description;
        DarkBox.IsChecked = _working.IsDark;
        _loading = false;
        RefreshSwatches();
        _runtime.PreviewTheme(_working);
    }

    private void BuildFields()
    {
        FieldsHost.Children.Clear();
        string? group = null;
        foreach (var field in ColorFields)
        {
            if (field.Group != group)
            {
                group = field.Group;
                FieldsHost.Children.Add(new Label { Content = group, Margin = new Thickness(0, 8, 0, 4) });
            }

            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6), LastChildFill = true };
            var swatch = new Rectangle
            {
                Width = 36,
                Height = 22,
                RadiusX = 2,
                RadiusY = 2,
                Stroke = (Brush)FindResource("BorderBrushKey"),
                StrokeThickness = 1,
                Cursor = Cursors.Hand,
                Tag = field.Property,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Click to pick a color"
            };
            swatch.MouseLeftButtonUp += Swatch_Click;
            DockPanel.SetDock(swatch, Dock.Left);
            var hex = new TextBox
            {
                Width = 88,
                Tag = field.Property,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            hex.LostFocus += Hex_LostFocus;
            DockPanel.SetDock(hex, Dock.Right);
            row.Children.Add(swatch);
            row.Children.Add(hex);
            row.Children.Add(new TextBlock
            {
                Text = field.Label,
                Foreground = (Brush)FindResource("PrimaryTextBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });
            FieldsHost.Children.Add(row);
        }
    }

    private void RefreshSwatches()
    {
        if (_working is null)
        {
            return;
        }

        foreach (var row in FieldsHost.Children.OfType<DockPanel>())
        {
            var swatch = row.Children.OfType<Rectangle>().FirstOrDefault();
            var hex = row.Children.OfType<TextBox>().FirstOrDefault();
            if (swatch?.Tag is not string property || hex is null)
            {
                continue;
            }

            var value = ReadColor(property) ?? "#000000";
            hex.Text = value;
            if (ThemeApplication.TryParse(value, out var color))
            {
                swatch.Fill = new SolidColorBrush(color);
            }
        }

        PaletteHost.Children.Clear();
        var palette = _working.Palette;
        for (var i = 0; i < Math.Min(16, palette.Length); i++)
        {
            var index = i;
            var chip = new Rectangle
            {
                Width = 28,
                Height = 22,
                Margin = new Thickness(0, 0, 6, 6),
                RadiusX = 2,
                RadiusY = 2,
                Stroke = (Brush)FindResource("BorderBrushKey"),
                StrokeThickness = 1,
                Cursor = Cursors.Hand,
                ToolTip = index.ToString() + "  " + palette[index],
                Tag = index
            };
            if (ThemeApplication.TryParse(palette[index], out var color))
            {
                chip.Fill = new SolidColorBrush(color);
            }

            chip.MouseLeftButtonUp += Palette_Click;
            PaletteHost.Children.Add(chip);
        }
    }

    private void Swatch_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Rectangle { Tag: string property } || _working is null)
        {
            return;
        }

        if (PickHex(ReadColor(property), out var hex))
        {
            WriteColor(property, hex);
        }
    }

    private void Palette_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Rectangle { Tag: int index } || _working is null)
        {
            return;
        }

        if (PickHex(_working.Palette[index], out var hex))
        {
            _working.Palette[index] = hex;
            MarkDirty();
        }
    }

    private void Hex_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { Tag: string property } box || _working is null)
        {
            return;
        }

        if (ThemeApplication.TryParse(box.Text, out var color))
        {
            WriteColor(property, Format(color));
        }
        else
        {
            box.Text = ReadColor(property);
        }
    }

    private void Meta_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading || _working is null)
        {
            return;
        }

        _working.Name = NameBox.Text.Trim();
        _working.Description = DescBox.Text.Trim();
        _dirty = true;
    }

    private void Dark_Click(object sender, RoutedEventArgs e)
    {
        if (_loading || _working is null)
        {
            return;
        }

        _working.IsDark = DarkBox.IsChecked == true;
        MarkDirty();
    }

    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        if (_working is null)
        {
            return;
        }

        var copy = _runtime.Themes.CloneAsNew(_working, _working.Name + " copy");
        _runtime.SaveTheme(copy);
        _dirty = false;
        ReloadList(copy.Id);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (_working is null || !ThemeCatalog.IsBuiltIn(_working.Id))
        {
            MessageBox.Show(this, "Reset only applies to the built-in themes. Duplicate first if you want a personal copy.",
                "Theme editor");
            return;
        }

        if (MessageBox.Show(this, "Restore factory colors for " + _working.Name + "?",
                "Reset theme", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var restored = _runtime.Themes.ResetBuiltIn(_working.Id);
        _runtime.SaveTheme(restored);
        _dirty = false;
        ReloadList(restored.Id);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_working is null)
        {
            return;
        }

        if (ThemeCatalog.IsBuiltIn(_working.Id))
        {
            MessageBox.Show(this, "Built-in themes cannot be deleted. Duplicate them, then delete the copy.", "Theme editor");
            return;
        }

        if (MessageBox.Show(this, "Delete " + _working.Name + "?", "Delete theme",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var id = _working.Id;
        _dirty = false;
        _runtime.Themes.Delete(id);
        var next = _runtime.Theme.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ? "amoled-black" : _runtime.Theme.Id;
        _runtime.ApplyTheme(next);
        ReloadList(_runtime.Theme.Id);
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_runtime.Themes.UserThemesDirectory);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _runtime.Themes.UserThemesDirectory,
            UseShellExecute = true
        });
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_working is null)
        {
            return;
        }

        _runtime.SaveTheme(_working);
        _dirty = false;
        ReloadList(_working.Id);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ThemeEditor_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_dirty && _working is not null)
        {
            var answer = MessageBox.Show(this, "Save changes to " + _working.Name + "?",
                "Theme editor", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (answer == MessageBoxResult.Yes)
            {
                _runtime.SaveTheme(_working);
            }
            else
            {
                _runtime.ApplyTheme(_startedThemeId);
            }
        }

        _dirty = false;
    }

    private void WriteColor(string property, string hex)
    {
        if (_working is null)
        {
            return;
        }

        typeof(ThemeUiColors).GetProperty(property, BindingFlags.Public | BindingFlags.Instance)
            ?.SetValue(_working.Ui, hex);
        MarkDirty();
    }

    private string? ReadColor(string property) =>
        typeof(ThemeUiColors).GetProperty(property, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(_working?.Ui) as string;

    private void MarkDirty()
    {
        _dirty = true;
        RefreshSwatches();
        if (_working is not null)
        {
            _runtime.PreviewTheme(_working);
        }
    }

    private bool PickHex(string? current, out string hex)
    {
        hex = current ?? "#000000";
        ThemeApplication.TryParse(hex, out var start);
        var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(start.A, start.R, start.G, start.B)
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return false;
        }

        hex = Format(Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B));
        return true;
    }

    private static string Format(Color color) =>
        "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
}
