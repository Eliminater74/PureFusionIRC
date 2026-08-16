using System.Collections.ObjectModel;
using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Irc;
using PureFusionIRC.Core.Logging;
using PureFusionIRC.Core.Models;
using PureFusionIRC.Core.Scripting;
using PureFusionIRC.Core.Settings;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.Core;

/// <summary>Owns settings, themes, scripts, and the set of live IRC sessions.</summary>
public sealed class ClientRuntime : IAsyncDisposable
{
    public ClientRuntime(string? dataRoot = null)
    {
        Store = new SettingsStore(dataRoot);
        Store.EnsureLayout();
        Themes = new ThemeCatalog(Store.ThemesDir);
        Themes.SeedUserCopies();
        Scripts = new JavascriptScriptHost();
        Plugins = new PluginFolder(Store.PluginsDir);
        Plugins.Ensure();
        Document = Store.Load();
        Dcc = new Dcc.DccEngine(Store);
        Logs = new BufferLogWriter(Store);
        Theme = Themes.Get(Document.App.ThemeId);
        Scripts.Error += (_, e) => LastScriptError = e.File + ": " + e.Message;
        Scripts.LoadDirectory(Store.ScriptsDir);
    }

    public SettingsStore Store { get; }
    public SettingsDocument Document { get; private set; }
    public ThemeCatalog Themes { get; }
    public ThemeDefinition Theme { get; private set; }
    public JavascriptScriptHost Scripts { get; }
    public PluginFolder Plugins { get; }
    public Dcc.DccEngine Dcc { get; }
    public BufferLogWriter Logs { get; }
    public ObservableCollection<IrcSession> Sessions { get; } = new();
    public string? LastScriptError { get; private set; }

    public event EventHandler? ThemeChanged;
    public event EventHandler<IrcSession>? SessionAdded;

    public void Save() => Store.Save(Document);

    public ThemeDefinition ApplyTheme(string themeId)
    {
        Theme = Themes.Get(themeId);
        Document.App.ThemeId = Theme.Id;
        foreach (var session in Sessions)
        {
            session.Theme = Theme;
        }

        Save();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
        return Theme;
    }

    public NetworkProfile AddNetwork(NetworkProfile profile)
    {
        Document.Networks.Add(profile);
        Save();
        return profile;
    }

    public void RemoveNetwork(Guid id)
    {
        Document.Networks.RemoveAll(n => n.Id == id);
        Save();
    }

    public async Task<IrcSession> ConnectAsync(NetworkProfile network, CancellationToken cancellationToken = default)
    {
        var session = new IrcSession(Guid.NewGuid().ToString("N"), network, Document.App.Identity, Document.App, Theme);
        session.Dcc = Dcc;
        session.Logs = Logs;
        session.Synchronization = SynchronizationContext.Current;
        session.ThemeRequested += (_, e) => ApplyTheme(e.ThemeId);
        session.PersistRequested += (_, _) => Save();
        session.LineAdded += (_, e) =>
        {
            Logs.Write(session, e.Buffer, e.Line);
            ForwardScriptLine(session, e);
        };
        session.StateChanged += (_, _) =>
        {
            if (session.State == SessionState.Connected)
            {
                Scripts.Emit("connect", new Dictionary<string, object?>
                {
                    ["network"] = network.Name,
                    ["nick"] = session.CurrentNick
                });
            }
        };

        Sessions.Add(session);
        SessionAdded?.Invoke(this, session);
        Scripts.Attach(session, async raw =>
        {
            await session.Commands.ExecuteAsync(session, session.ServerBuffer, raw).ConfigureAwait(false);
        }, text => session.Print(session.ServerBuffer, ChatLineKind.Info, text));

        await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var session in Sessions.ToArray())
        {
            await session.DisconnectAsync().ConfigureAwait(false);
        }
    }

    public void Export(string zipPath) => SettingsPack.Export(Store, zipPath);

    public void Import(string zipPath)
    {
        SettingsPack.Import(Store, zipPath);
        Document = Store.Load();
        ApplyTheme(Document.App.ThemeId);
        Scripts.LoadDirectory(Store.ScriptsDir);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAllAsync().ConfigureAwait(false);
        Dcc.Dispose();
        Logs.Dispose();
        foreach (var session in Sessions)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }

    private void ForwardScriptLine(IrcSession session, LineEventArgs e)
    {
        if (e.Line.Kind is not ChatLineKind.Message and not ChatLineKind.Action)
        {
            return;
        }

        Scripts.Emit("message", new Dictionary<string, object?>
        {
            ["network"] = session.Network.Name,
            ["target"] = e.Buffer.Name,
            ["nick"] = e.Line.Nick,
            ["text"] = e.Line.Text,
            ["self"] = e.Line.IsSelf
        });
    }
}
