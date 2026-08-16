using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using PureFusionIRC.App.Theming;
using PureFusionIRC.App.Tray;
using PureFusionIRC.App.Windows;
using PureFusionIRC.Core;
using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Irc;
using PureFusionIRC.Core.Models;
using PureFusionIRC.Core.Text;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.App;

public partial class MainWindow : Window
{
    private readonly ClientRuntime _runtime;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;
    private int _atTokenStart;
    private IrcSession? _session;
    private IrcBuffer? _buffer;
    private bool _editingTopic;
    private bool _exitRequested;
    private TrayController? _tray;

    public AppSettings Settings => _runtime.Document.App;

    public MainWindow() : this(new ClientRuntime())
    {
    }

    public MainWindow(ClientRuntime runtime)
    {
        _runtime = runtime;
        InitializeComponent();
        ApplyTheme(_runtime.Theme);
        BuildThemeMenu();
        BufferTree.ItemsSource = _runtime.Sessions;
        Chat.Configure(_runtime.Theme, _runtime.Document.App);
        Chat.ReplyNick += InsertReplyPrefix;
        Chat.QueryNick += nick => _ = NickCommandFromChatAsync("/query {0}", nick);
        Chat.WhoisNick += nick => _ = NickCommandFromChatAsync("/whois {0}", nick);
        ShowTreeItem.IsChecked = _runtime.Document.App.ShowTree;
        ShowNicksItem.IsChecked = _runtime.Document.App.ShowNickList;
        ShowToolbarItem.IsChecked = _runtime.Document.App.ShowToolbar;
        ApplyLayoutFlags();
        _runtime.ThemeChanged += (_, _) => Dispatcher.Invoke(() => ApplyTheme(_runtime.Theme));
        _runtime.SessionAdded += (_, session) => Dispatcher.Invoke(() => HookSession(session));
        _tray = new TrayController(
            () => Dispatcher.Invoke(RestoreFromTray),
            () => Dispatcher.Invoke(() => Networks_Click(this, new RoutedEventArgs())),
            () => Dispatcher.Invoke(ExitFromTray));
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized && Settings.MinimizeToTray)
            {
                HideToTray();
            }
        };
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    public void OpenNetworks() => Networks_Click(this, new RoutedEventArgs());

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InputBox.Focus();
        Dispatcher.BeginInvoke(() => _ = AutoStartAsync(), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private async Task AutoStartAsync()
    {
        var autoConnect = _runtime.Document.Networks
            .Where(n => n.Enabled && n.ConnectOnStartup && n.Servers.Count > 0)
            .ToList();
        foreach (var network in autoConnect)
        {
            if (_runtime.Sessions.Any(s => s.Network.Id == network.Id))
            {
                continue;
            }

            await ConnectAsync(network).ConfigureAwait(true);
        }

        if (_runtime.Sessions.Count == 0)
        {
            Networks_Click(this, new RoutedEventArgs());
        }
    }

    private void HookSession(IrcSession session)
    {
        session.Synchronization = SynchronizationContext.Current;
        session.LineAdded += (_, e) => Dispatcher.Invoke(() =>
        {
            if (ReferenceEquals(_buffer, e.Buffer))
            {
                Chat.Append(e.Buffer, e.Line);
            }

            if (Settings.TrayNotifications)
            {
                var watchingThisBuffer = IsVisible && IsActive && WindowState != WindowState.Minimized
                    && ReferenceEquals(_buffer, e.Buffer);
                _tray?.NotifyChat(e.Buffer, e.Line, watchingThisBuffer);
            }

            RefreshStatus();
        });
        session.StateChanged += (_, _) => Dispatcher.Invoke(() =>
        {
            RefreshStatus();
            if (session.State == SessionState.Disconnected &&
                !session.UserRequestedDisconnect &&
                Settings.TrayNotifications)
            {
                _tray?.Notify(
                    "Disconnected",
                    session.Network.Name + " dropped.",
                    IsVisible && IsActive && WindowState != WindowState.Minimized);
            }
        });
        session.BufferOpened += (_, buffer) => Dispatcher.Invoke(() =>
        {
            SelectBuffer(session, buffer);
        });
        BufferTree.Items.Refresh();
        _session = session;
        SelectBuffer(session, session.ServerBuffer);
    }

    private void SelectBuffer(IrcSession session, IrcBuffer buffer)
    {
        if (_buffer is not null)
        {
            _buffer.PropertyChanged -= OnBufferPropertyChanged;
        }

        _session = session;
        _buffer = buffer;
        _buffer.PropertyChanged += OnBufferPropertyChanged;
        _editingTopic = false;
        Chat.Show(buffer);
        NickList.ItemsSource = buffer.Nicks;
        Title = $"{buffer.Name} — PureFusionIRC";
        InputPrompt.Text = buffer.Kind == BufferKind.Channel ? buffer.Name : ">";
        RefreshPinnedBars();
        RefreshStatus();
        RefreshAutoJoinUi();
    }

    private void OnBufferPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IrcBuffer.Topic) or nameof(IrcBuffer.UserCount) or nameof(IrcBuffer.Nicks))
        {
            RefreshPinnedBars();
            RefreshStatus();
        }
    }

    private void RefreshPinnedBars()
    {
        if (_buffer is null)
        {
            TopicChannelLabel.Text = "";
            TopicBar.Text = "";
            TopicBar.IsReadOnly = true;
            NickHeader.Text = "Nicks";
            return;
        }

        TopicChannelLabel.Text = _buffer.Kind == BufferKind.Channel ? _buffer.Name : "";
        TopicBar.IsReadOnly = _buffer.Kind != BufferKind.Channel;
        if (!_editingTopic)
        {
            TopicBar.Text = _buffer.Kind == BufferKind.Channel
                ? _buffer.Topic ?? string.Empty
                : _buffer.Kind == BufferKind.Query
                    ? "Query with " + _buffer.Name
                    : (_session?.Network.Name ?? "Server");
        }

        NickHeader.Text = _buffer.Kind == BufferKind.Channel ? $"Nicks ({_buffer.UserCount})" : "Nicks";
    }

    private void TopicBar_GotFocus(object sender, RoutedEventArgs e) => _editingTopic = true;

    private void TopicBar_LostFocus(object sender, RoutedEventArgs e)
    {
        _editingTopic = false;
        RefreshPinnedBars();
    }

    private async void TopicBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (_session is null || _buffer is null || _buffer.Kind != BufferKind.Channel)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            _editingTopic = false;
            RefreshPinnedBars();
            InputBox.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        var topic = TopicBar.Text;
        _editingTopic = false;
        await _session.Commands.ExecuteAsync(_session, _buffer, "/topic " + topic).ConfigureAwait(true);
        InputBox.Focus();
    }

    private void ApplyTheme(ThemeDefinition theme)
    {
        ThemeApplication.Apply(theme, Application.Current.Resources);
        Chat.Configure(theme, _runtime.Document.App);
        Background = (System.Windows.Media.Brush)FindResource("WindowBackgroundBrush");
    }

    private void BuildThemeMenu()
    {
        ThemeMenu.Items.Clear();
        foreach (var theme in _runtime.Themes.LoadAll())
        {
            var item = new MenuItem { Header = theme.Name, Tag = theme.Id, IsCheckable = true };
            item.IsChecked = string.Equals(theme.Id, _runtime.Theme.Id, StringComparison.OrdinalIgnoreCase);
            item.Click += (_, _) =>
            {
                _runtime.ApplyTheme((string)item.Tag);
                BuildThemeMenu();
                if (_buffer is not null)
                {
                    Chat.Show(_buffer);
                }
            };
            ThemeMenu.Items.Add(item);
        }
    }

    private async void Networks_Click(object sender, RoutedEventArgs e)
    {
        var window = new NetworkWindow(_runtime) { Owner = this };
        if (window.ShowDialog() == true && window.ConnectTarget is not null)
        {
            await ConnectAsync(window.ConnectTarget).ConfigureAwait(true);
        }
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        var network = _runtime.Document.Networks.FirstOrDefault(n => n.Enabled);
        if (network is null)
        {
            Networks_Click(sender, e);
            return;
        }

        await ConnectAsync(network).ConfigureAwait(true);
    }

    private async Task ConnectAsync(NetworkProfile network)
    {
        try
        {
            var session = await _runtime.ConnectAsync(network).ConfigureAwait(true);
            session.Synchronization ??= SynchronizationContext.Current;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Connect failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        if (_session is not null)
        {
            await _session.DisconnectAsync().ConfigureAwait(true);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => ExitFromTray();

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;
        Topmost = false;
    }

    private void ExitFromTray()
    {
        _exitRequested = true;
        Close();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!_exitRequested && Settings.CloseToTray)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        _tray?.Dispose();
        _runtime.Save();
        _ = _runtime.DisposeAsync().AsTask();
    }

    private void ChannelMenu_Opened(object sender, RoutedEventArgs e)
    {
        var can = CanEditAutoJoin();
        AutoJoinChannelItem.IsEnabled = can;
        AutoJoinChannelItem.IsChecked = can && _session!.Network.HasAutoJoin(_buffer!.Name);
    }

    private void BufferTreeMenu_Opened(object sender, RoutedEventArgs e)
    {
        var can = CanEditAutoJoin();
        TreeAutoJoinItem.IsEnabled = can;
        TreeAutoJoinItem.IsChecked = can && _session!.Network.HasAutoJoin(_buffer!.Name);
    }

    private void ToggleAutoJoin_Click(object sender, RoutedEventArgs e)
    {
        if (!CanEditAutoJoin())
        {
            return;
        }

        var enable = sender is MenuItem { IsCheckable: true } item
            ? item.IsChecked == true
            : !_session!.Network.HasAutoJoin(_buffer!.Name);
        ApplyAutoJoinPreference(enable);
    }

    private void ToolbarAutoJoin_Click(object sender, RoutedEventArgs e)
    {
        if (!CanEditAutoJoin())
        {
            MessageBox.Show(this, "Join a channel first, then use Auto-join.", "Auto-join",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ApplyAutoJoinPreference(!_session!.Network.HasAutoJoin(_buffer!.Name));
    }

    private bool CanEditAutoJoin() =>
        _session is not null && _buffer is { Kind: BufferKind.Channel };

    private void ApplyAutoJoinPreference(bool enable)
    {
        if (!CanEditAutoJoin())
        {
            return;
        }

        _session!.Network.SetAutoJoin(_buffer!.Name, enable);
        _runtime.Save();
        RefreshAutoJoinUi();
        _session.Print(_buffer, ChatLineKind.Info,
            enable
                ? "Added " + _buffer.Name + " to auto-join for " + _session.Network.Name
                : "Removed " + _buffer.Name + " from auto-join for " + _session.Network.Name);
    }

    private void RefreshAutoJoinUi()
    {
        var can = CanEditAutoJoin();
        var on = can && _session!.Network.HasAutoJoin(_buffer!.Name);
        AutoJoinChannelItem.IsEnabled = can;
        AutoJoinChannelItem.IsChecked = on;
        AutoJoinButton.IsEnabled = can;
        AutoJoinButton.Content = on ? "Auto-join ✓" : "Auto-join";
    }

    private void ToggleTree_Click(object sender, RoutedEventArgs e)
    {
        _runtime.Document.App.ShowTree = ShowTreeItem.IsChecked == true;
        ApplyLayoutFlags();
        _runtime.Save();
    }

    private void ToggleNicks_Click(object sender, RoutedEventArgs e)
    {
        _runtime.Document.App.ShowNickList = ShowNicksItem.IsChecked == true;
        ApplyLayoutFlags();
        _runtime.Save();
    }

    private void ToggleToolbar_Click(object sender, RoutedEventArgs e)
    {
        _runtime.Document.App.ShowToolbar = ShowToolbarItem.IsChecked == true;
        ApplyLayoutFlags();
        _runtime.Save();
    }

    private void ApplyLayoutFlags()
    {
        TreeColumn.Width = _runtime.Document.App.ShowTree ? new GridLength(220) : new GridLength(0);
        NickColumn.Width = _runtime.Document.App.ShowNickList ? new GridLength(180) : new GridLength(0);
        ToolbarTray.Visibility = _runtime.Document.App.ShowToolbar ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Options_Click(object sender, RoutedEventArgs e)
    {
        var window = new OptionsWindow(_runtime) { Owner = this };
        if (window.ShowDialog() == true)
        {
            ApplyTheme(_runtime.Theme);
            Chat.Configure(_runtime.Theme, _runtime.Document.App);
            if (_buffer is not null)
            {
                Chat.Show(_buffer);
            }
        }
    }

    private void ReloadScripts_Click(object sender, RoutedEventArgs e)
    {
        _runtime.Scripts.LoadDirectory(_runtime.Store.ScriptsDir);
        MessageBox.Show(this,
            $"Loaded {_runtime.Scripts.LoadedCount} script(s)." +
            (_runtime.LastScriptError is null ? "" : Environment.NewLine + _runtime.LastScriptError),
            "Scripts");
    }

    private void OpenScripts_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_runtime.Store.ScriptsDir);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _runtime.Store.ScriptsDir,
            UseShellExecute = true
        });
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PureFusion settings pack (*.zip)|*.zip",
            FileName = "purefusion-settings.zip"
        };
        if (dialog.ShowDialog(this) == true)
        {
            _runtime.Save();
            _runtime.Export(dialog.FileName);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PureFusion settings pack (*.zip)|*.zip" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _runtime.Import(dialog.FileName);
        ApplyTheme(_runtime.Theme);
        BuildThemeMenu();
        MessageBox.Show(this, "Settings imported. Connect again to use network changes.", "Import");
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "PureFusionIRC " + GetProductVersion() + "\nWindows C# IRC client inspired by mIRC, with a full theme engine.\nDefault theme: AMOLED Black.\nScripts: JavaScript (.pf.js), not mIRC script.\n\nMIT License",
            "About PureFusionIRC");
    }

    private void BufferTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        switch (e.NewValue)
        {
            case IrcBuffer buffer:
            {
                var session = _runtime.Sessions.FirstOrDefault(s => s.Id == buffer.SessionId);
                if (session is not null)
                {
                    SelectBuffer(session, buffer);
                }

                break;
            }
            case IrcSession session:
                SelectBuffer(session, session.ServerBuffer);
                break;
        }
    }

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (NickPopup.IsOpen)
        {
            if (e.Key == Key.Escape)
            {
                HideNickPicker();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down)
            {
                MoveNickPicker(1);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up)
            {
                MoveNickPicker(-1);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Enter or Key.Tab or Key.Right)
            {
                if (AcceptNickPicker())
                {
                    e.Handled = true;
                }
            }
        }
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            if (_history.Count == 0)
            {
                return;
            }

            _historyIndex = _historyIndex < 0 ? _history.Count - 1 : Math.Max(0, _historyIndex - 1);
            InputBox.Text = _history[_historyIndex];
            InputBox.CaretIndex = InputBox.Text.Length;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            if (_historyIndex < 0)
            {
                return;
            }

            _historyIndex++;
            if (_historyIndex >= _history.Count)
            {
                _historyIndex = -1;
                InputBox.Clear();
            }
            else
            {
                InputBox.Text = _history[_historyIndex];
                InputBox.CaretIndex = InputBox.Text.Length;
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            if (NickPopup.IsOpen && AcceptNickPicker())
            {
                e.Handled = true;
                return;
            }

            TryNickComplete();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && NickPopup.IsOpen && AcceptNickPicker())
        {
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter || _session is null || _buffer is null)
        {
            return;
        }

        var text = InputBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _history.Add(text);
        _historyIndex = -1;
        InputBox.Clear();
        var result = await _session.Commands.ExecuteAsync(_session, _buffer, text).ConfigureAwait(true);
        if (result.Error is not null)
        {
            _session.Print(_buffer, ChatLineKind.Error, result.Error);
        }
        else if (result.Info is not null)
        {
            _session.Print(_buffer, ChatLineKind.Info, result.Info);
        }
    }

    private void TryNickComplete()
    {
        if (_buffer is null)
        {
            return;
        }

        var text = InputBox.Text;
        var caret = InputBox.CaretIndex;
        var start = caret;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
        {
            start--;
        }

        var prefix = text[start..caret];
        if (prefix.Length == 0)
        {
            return;
        }

        var match = _buffer.Nicks.Select(n => n.Nick)
            .FirstOrDefault(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return;
        }

        var insert = start == 0 ? match + ": " : match;
        InputBox.Text = text[..start] + insert + text[caret..];
        InputBox.CaretIndex = start + insert.Length;
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateNickPicker();

    private void UpdateNickPicker()
    {
        if (_buffer is null || _buffer.Kind != BufferKind.Channel ||
            !NickMatcher.TryGetAtToken(InputBox.Text, InputBox.CaretIndex, out _atTokenStart, out var query))
        {
            HideNickPicker();
            return;
        }

        var hits = NickMatcher.Filter(_buffer.Nicks.Select(n => n.Nick), query);
        if (hits.Count == 0)
        {
            HideNickPicker();
            return;
        }

        NickSuggestList.ItemsSource = hits;
        NickSuggestList.SelectedIndex = 0;
        NickPopup.IsOpen = true;
    }

    private void HideNickPicker()
    {
        NickPopup.IsOpen = false;
        NickSuggestList.ItemsSource = null;
    }

    private void MoveNickPicker(int delta)
    {
        var count = NickSuggestList.Items.Count;
        if (count == 0)
        {
            return;
        }

        var next = NickSuggestList.SelectedIndex + delta;
        if (next < 0)
        {
            next = count - 1;
        }
        else if (next >= count)
        {
            next = 0;
        }

        NickSuggestList.SelectedIndex = next;
        NickSuggestList.ScrollIntoView(NickSuggestList.SelectedItem);
    }

    private bool AcceptNickPicker()
    {
        if (NickSuggestList.SelectedItem is not string nick)
        {
            return false;
        }

        var original = InputBox.Text;
        var caret = InputBox.CaretIndex;
        InputBox.Text = NickMatcher.InsertNick(original, _atTokenStart, caret, nick);
        var colon = _atTokenStart == 0 || original[.._atTokenStart].All(char.IsWhiteSpace);
        InputBox.CaretIndex = _atTokenStart + nick.Length + (colon ? 2 : 1);
        HideNickPicker();
        return true;
    }

    private void NickSuggest_Click(object sender, MouseButtonEventArgs e)
    {
        if (NickSuggestList.SelectedItem is string)
        {
            AcceptNickPicker();
            InputBox.Focus();
        }
    }

    private async void NickList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedNick() is { } nick && _session is not null)
        {
            await _session.Commands.ExecuteAsync(_session, _buffer ?? _session.ServerBuffer, "/query " + nick)
                .ConfigureAwait(true);
        }
    }

    private string? SelectedNick() => NickList.SelectedItem is NickEntry entry ? entry.Nick : null;

    private void InsertReplyPrefix(string nick)
    {
        InputBox.Focus();
        var rest = InputBox.Text.TrimStart();
        InputBox.Text = string.IsNullOrEmpty(rest) ? nick + ": " : nick + ": " + rest;
        InputBox.CaretIndex = InputBox.Text.Length;
    }

    private async Task NickCommandFromChatAsync(string template, string nick)
    {
        if (_session is null)
        {
            return;
        }

        await _session.Commands.ExecuteAsync(_session, _buffer ?? _session.ServerBuffer, string.Format(template, nick))
            .ConfigureAwait(true);
    }

    private async Task NickCommandAsync(string template)
    {
        if (SelectedNick() is not { } nick || _session is null || _buffer is null)
        {
            return;
        }

        await _session.Commands.ExecuteAsync(_session, _buffer, string.Format(template, nick)).ConfigureAwait(true);
    }

    private async void NickQuery_Click(object sender, RoutedEventArgs e) => await NickCommandAsync("/query {0}");
    private async void NickWhois_Click(object sender, RoutedEventArgs e) => await NickCommandAsync("/whois {0}");
    private async void NickOp_Click(object sender, RoutedEventArgs e) => await ChannelModeAsync("+o");
    private async void NickDeop_Click(object sender, RoutedEventArgs e) => await ChannelModeAsync("-o");
    private async void NickVoice_Click(object sender, RoutedEventArgs e) => await ChannelModeAsync("+v");
    private async void NickDevoice_Click(object sender, RoutedEventArgs e) => await ChannelModeAsync("-v");
    private async void NickKick_Click(object sender, RoutedEventArgs e) => await NickCommandAsync("/kick {0}");
    private async void NickBan_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedNick() is not { } nick || _session is null || _buffer is null)
        {
            return;
        }

        await _session.Commands.ExecuteAsync(_session, _buffer, $"/quote MODE {_buffer.Name} +b {nick}!*@*").ConfigureAwait(true);
    }

    private async Task ChannelModeAsync(string mode)
    {
        if (SelectedNick() is not { } nick || _session is null || _buffer is null)
        {
            return;
        }

        await _session.Commands.ExecuteAsync(_session, _buffer, $"/mode {_buffer.Name} {mode} {nick}").ConfigureAwait(true);
    }

    private void RefreshStatus()
    {
        StatusNick.Text = _session is null
            ? "(not connected)"
            : _session.State switch
            {
                SessionState.Connecting => "connecting…",
                SessionState.Registering => "waiting for server…",
                SessionState.Disconnecting => "disconnecting…",
                SessionState.Disconnected => _session.CurrentNick + " (offline)",
                _ => _session.CurrentNick
            };
        StatusLag.Text = _session?.State switch
        {
            SessionState.Connecting => "Connecting",
            SessionState.Registering => "Waiting (IRCnet proxy/ident scan)",
            SessionState.Connected => $"Lag: {_session.Lag.TotalMilliseconds:0} ms",
            _ => "Lag: —"
        };
        StatusUsers.Text = _buffer?.Kind == BufferKind.Channel ? $"{_buffer.UserCount} users" : "";
        StatusTopic.Text = _buffer?.Topic ?? _session?.Network.Name ?? "PureFusionIRC";
        if (_buffer is not null && _buffer.Kind == BufferKind.Channel)
        {
            NickHeader.Text = $"Nicks ({_buffer.UserCount})";
            NickList.ItemsSource = _buffer.Nicks;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            Networks_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }

    private static string GetProductVersion()
    {
        var info = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(info))
        {
            return typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0.0-B1";
        }

        var plus = info.IndexOf('+');
        return plus < 0 ? info : info[..plus];
    }
}
