using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Commands;
using PureFusionIRC.Core.Models;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.Core.Irc;

public enum SessionState
{
    Disconnected,
    Connecting,
    Registering,
    Connected,
    Disconnecting
}

public sealed class ThemeRequestEventArgs : EventArgs
{
    public ThemeRequestEventArgs(string themeId) => ThemeId = themeId;
    public string ThemeId { get; }
}

public sealed class LineEventArgs : EventArgs
{
    public LineEventArgs(IrcBuffer buffer, ChatLine line)
    {
        Buffer = buffer;
        Line = line;
    }

    public IrcBuffer Buffer { get; }
    public ChatLine Line { get; }
}

/// <summary>One connection to one IRC network: register, dispatch, buffers, CAP/SASL.</summary>
public sealed partial class IrcSession : IAsyncDisposable
{
    private readonly IrcConnection _connection = new();
    private CancellationTokenSource? _runCts;
    private Task? _readTask;
    private readonly List<string> _pendingCaps = new();
    private bool _capEnded;
    private bool _saslDone;
    private long _lagSentUnixMs;
    private int _nickTries;

    public IrcSession(string id, NetworkProfile network, UserIdentity identity, AppSettings settings, ThemeDefinition theme)
    {
        Id = id;
        Network = network;
        Identity = identity;
        Settings = settings;
        Theme = theme;
        CurrentNick = network.NickOverride is { Length: > 0 } n ? n : identity.Nick;
        ServerBuffer = GetOrCreate(BufferKind.Server, network.Name);
        Commands = new CommandProcessor();
        Buffers.Add(ServerBuffer);
    }

    public string Id { get; }
    public NetworkProfile Network { get; }
    public UserIdentity Identity { get; }
    public AppSettings Settings { get; }
    public ThemeDefinition Theme { get; set; }
    public CommandProcessor Commands { get; }
    public string CurrentNick { get; private set; }
    public SessionState State { get; private set; } = SessionState.Disconnected;
    public IrcBuffer ServerBuffer { get; }
    public ObservableCollection<IrcBuffer> Buffers { get; } = new();
    public Dictionary<string, string> ISupport { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string PrefixLetters { get; private set; } = "ov";
    public string PrefixSymbols { get; private set; } = "@+";
    public TimeSpan Lag { get; private set; }
    public string? UserModes { get; private set; }
    public string? NetworkName => ISupport.GetValueOrDefault("NETWORK") ?? Network.Name;
    public IReadOnlyList<string> RequestedCapabilities { get; private set; } = Array.Empty<string>();

    public event EventHandler? StateChanged;
    public event EventHandler<IrcBuffer>? BufferOpened;
    public event EventHandler<IrcBuffer>? BufferClosed;
    public event EventHandler<LineEventArgs>? LineAdded;
    public event EventHandler<ThemeRequestEventArgs>? ThemeRequested;
    public event EventHandler<string>? RawSent;
    public event EventHandler<string>? RawReceived;

    public IrcBuffer GetOrCreate(BufferKind kind, string name)
    {
        var existing = Buffers.FirstOrDefault(b =>
            b.Kind == kind && string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var buffer = new IrcBuffer(Id, kind, name);
        Buffers.Add(buffer);
        BufferOpened?.Invoke(this, buffer);
        return buffer;
    }

    public void OpenQuery(string nick)
    {
        var buffer = GetOrCreate(BufferKind.Query, nick);
        Print(buffer, ChatLineKind.Info, "Query with " + nick);
    }

    public void CloseBuffer(IrcBuffer buffer)
    {
        if (buffer.Kind == BufferKind.Server)
        {
            return;
        }

        Buffers.Remove(buffer);
        BufferClosed?.Invoke(this, buffer);
    }

    public void Print(IrcBuffer buffer, ChatLineKind kind, string text, string? nick = null, bool self = false)
    {
        var highlight = !self && IsHighlight(text);
        var line = new ChatLine(DateTimeOffset.Now, kind, nick, text, buffer.Name, self, highlight);
        buffer.Append(line, Settings.MaxBufferLines);
        if (highlight)
        {
            buffer.Activity = BufferActivity.Highlight;
        }
        else if (kind is ChatLineKind.Message or ChatLineKind.Action or ChatLineKind.Notice)
        {
            buffer.Activity = BufferActivity.Message;
        }

        LineAdded?.Invoke(this, new LineEventArgs(buffer, line));
    }

    public void RequestTheme(string themeId) =>
        ThemeRequested?.Invoke(this, new ThemeRequestEventArgs(themeId));

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (State is SessionState.Connecting or SessionState.Registering or SessionState.Connected)
        {
            return;
        }

        var server = Network.PrimaryServer;
        var endpoint = new IrcEndpoint(server.Host, server.Port, server.UseTls, server.AcceptInvalidCertificates);
        SetState(SessionState.Connecting);
        Print(ServerBuffer, ChatLineKind.Info, "Connecting to " + endpoint + " …");

        try
        {
            await _connection.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SocketException or IOException or TimeoutException or AuthenticationException)
        {
            Print(ServerBuffer, ChatLineKind.Error, "Connect failed: " + ex.Message);
            SetState(SessionState.Disconnected);
            throw;
        }

        _nickTries = 0;
        _capEnded = false;
        _saslDone = false;
        ISupport.Clear();
        SetState(SessionState.Registering);
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _readTask = Task.Run(() => ReadLoopAsync(_runCts.Token), CancellationToken.None);

        await SendRawAsync("CAP LS 302", cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(server.Password))
        {
            await SendRawAsync("PASS " + server.Password, cancellationToken).ConfigureAwait(false);
        }

        await SendRawAsync("NICK " + CurrentNick, cancellationToken).ConfigureAwait(false);
        var userMode = Identity.Invisible ? "8" : "0";
        await SendRawAsync($"USER {Identity.Username} {userMode} * :{Identity.RealName}", cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        SetState(SessionState.Disconnecting);
        if (_runCts is not null)
        {
            await _runCts.CancelAsync().ConfigureAwait(false);
        }

        await _connection.DisposeAsync().ConfigureAwait(false);
        SetState(SessionState.Disconnected);
        Print(ServerBuffer, ChatLineKind.Info, "Disconnected.");
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task QuitAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (_connection.IsConnected)
        {
            await SendRawAsync("QUIT :" + reason, cancellationToken).ConfigureAwait(false);
        }

        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SendRawAsync(string line, CancellationToken cancellationToken = default)
    {
        RawSent?.Invoke(this, line);
        await _connection.SendLineAsync(line, cancellationToken).ConfigureAwait(false);
    }

    public async Task PrivmsgAsync(string target, string text, CancellationToken cancellationToken = default)
    {
        await SendRawAsync("PRIVMSG " + target + " :" + text, cancellationToken).ConfigureAwait(false);
        var kind = target.StartsWith('#') || target.StartsWith('&') ? BufferKind.Channel : BufferKind.Query;
        var buffer = GetOrCreate(kind, target);
        Print(buffer, ChatLineKind.Message, text, CurrentNick, self: true);
    }

    public async Task ActionAsync(string target, string text, CancellationToken cancellationToken = default)
    {
        await SendRawAsync($"PRIVMSG {target} :\u0001ACTION {text}\u0001", cancellationToken).ConfigureAwait(false);
        var kind = target.StartsWith('#') || target.StartsWith('&') ? BufferKind.Channel : BufferKind.Query;
        Print(GetOrCreate(kind, target), ChatLineKind.Action, text, CurrentNick, self: true);
    }

    public Task CtcpRequestAsync(string nick, string payload, CancellationToken cancellationToken = default) =>
        SendRawAsync($"PRIVMSG {nick} :\u0001{payload}\u0001", cancellationToken);

    public Task SendLagPingAsync(CancellationToken cancellationToken = default)
    {
        _lagSentUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return SendRawAsync("PING :pf" + _lagSentUnixMs.ToString(CultureInfo.InvariantCulture), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _runCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var line in _connection.ReadLinesAsync(cancellationToken).ConfigureAwait(false))
            {
                RawReceived?.Invoke(this, line);
                if (IrcMessage.TryParse(line, out var message))
                {
                    await HandleAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on disconnect
        }
        catch (Exception ex)
        {
            Print(ServerBuffer, ChatLineKind.Error, "Connection dropped: " + ex.Message);
        }
        finally
        {
            if (State != SessionState.Disconnecting)
            {
                SetState(SessionState.Disconnected);
            }
        }
    }

    private void SetState(SessionState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool IsHighlight(string text)
    {
        if (text.Contains(CurrentNick, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Settings.HighlightWords.Any(word =>
            !string.IsNullOrWhiteSpace(word) &&
            text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private char PrefixForMode(char mode)
    {
        var index = PrefixLetters.IndexOf(mode);
        return index >= 0 && index < PrefixSymbols.Length ? PrefixSymbols[index] : '\0';
    }

    private static string? CtcpPayload(string text)
    {
        if (text.Length >= 2 && text[0] == '\u0001' && text[^1] == '\u0001')
        {
            return text[1..^1];
        }

        if (text.StartsWith('\u0001'))
        {
            return text[1..];
        }

        return null;
    }

    private static string EncodeSaslPlain(string account, string password)
    {
        var raw = "\0" + account + "\0" + password;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }
}
