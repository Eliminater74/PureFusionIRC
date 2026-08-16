using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using PureFusionIRC.Core.Irc;
using PureFusionIRC.Core.Models;
using PureFusionIRC.Core.Settings;

namespace PureFusionIRC.Core.Dcc;

/// <summary>
/// File DCC with reverse-first sending so typical home NAT works, plus classic connect-out receives.
/// </summary>
public sealed class DccEngine : IDisposable
{
    private readonly SettingsStore _store;
    private readonly SynchronizationContext? _sync;
    private int _token;

    public DccEngine(SettingsStore store)
    {
        _store = store;
        _sync = SynchronizationContext.Current;
        Transfers = [];
    }

    public ObservableCollection<DccTransfer> Transfers { get; }

    public event EventHandler<DccTransfer>? IncomingOffer;

    public string FolderFor(AppSettings settings)
    {
        var folder = string.IsNullOrWhiteSpace(settings.DccDownloadFolder)
            ? _store.TransfersDir
            : settings.DccDownloadFolder;
        Directory.CreateDirectory(folder);
        return folder;
    }

    public void HandleCtcp(IrcSession session, string from, string payload)
    {
        if (!session.Settings.DccEnabled || !DccParser.TryParse(payload, out var offer))
        {
            return;
        }

        offer.PeerNick = from;
        if (offer.Kind == DccCommandKind.Chat)
        {
            session.Print(session.GetOrCreate(PureFusionIRC.Core.Buffers.BufferKind.Query, from),
                PureFusionIRC.Core.Buffers.ChatLineKind.Info,
                from + " requested a direct chat (DCC CHAT). File send is supported; chat-over-DCC is not yet.");
            return;
        }

        if (offer.Kind == DccCommandKind.Send && !string.IsNullOrEmpty(offer.Token) && offer.Port > 0)
        {
            var pending = Transfers.FirstOrDefault(t =>
                t.Direction == DccDirection.Outgoing &&
                t.IsReverse &&
                t.Status is DccStatus.Waiting or DccStatus.Offered &&
                string.Equals(t.Token, offer.Token, StringComparison.Ordinal));
            if (pending is not null)
            {
                pending.Detail = "They opened a port. Connecting from here (simple for home networks).";
                _ = SendOnConnectedAsync(session, pending, offer);
                return;
            }
        }

        if (offer.Kind is DccCommandKind.Accept)
        {
            return;
        }

        if (offer.Kind != DccCommandKind.Send)
        {
            return;
        }

        var transfer = new DccTransfer
        {
            Direction = DccDirection.Incoming,
            PeerNick = from,
            FileName = offer.FileName,
            FileSize = offer.FileSize,
            IsReverse = offer.IsReverse,
            Token = offer.Token,
            Offer = offer,
            Session = session,
            Status = DccStatus.Offered,
            Detail = offer.IsReverse
                ? "They are behind a firewall, so we will open a port and they will connect."
                : "We connect to them. This usually works from home internet."
        };
        Add(transfer);
        var query = session.GetOrCreate(PureFusionIRC.Core.Buffers.BufferKind.Query, from);
        session.Print(query, PureFusionIRC.Core.Buffers.ChatLineKind.Info,
            $"{from} wants to send {offer.FileName} ({DccParser.FormatBytes(offer.FileSize)}). Open Transfers to Save or Decline.");
        IncomingOffer?.Invoke(this, transfer);
    }

    public async Task SendFileAsync(IrcSession session, string nick, string path, CancellationToken cancellationToken = default)
    {
        if (!session.Settings.DccEnabled)
        {
            throw new InvalidOperationException("File sending is turned off in Options.");
        }

        path = Path.GetFullPath(path);
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            throw new FileNotFoundException("File not found.", path);
        }

        var name = DccParser.SafeFileName(info.Name);
        var transfer = new DccTransfer
        {
            Direction = DccDirection.Outgoing,
            PeerNick = nick,
            FileName = name,
            FileSize = info.Length,
            FilePath = path,
            Session = session,
            Status = DccStatus.Waiting,
            IsReverse = session.Settings.DccPreferReverse,
            Token = Interlocked.Increment(ref _token).ToString(),
            Detail = session.Settings.DccPreferReverse
                ? "Asking them to open a port so you do not need port forwarding."
                : "Waiting for them to connect to you."
        };
        Add(transfer);

        if (transfer.IsReverse)
        {
            await session.CtcpRequestAsync(nick,
                    DccParser.FormatSend(name, 1, 0, info.Length, transfer.Token), cancellationToken)
                .ConfigureAwait(false);
            session.Print(session.GetOrCreate(PureFusionIRC.Core.Buffers.BufferKind.Query, nick),
                PureFusionIRC.Core.Buffers.ChatLineKind.Info,
                "Sending " + name + " to " + nick + " (modern reverse DCC).");
            _ = FallbackClassicIfNeededAsync(session, transfer, info);
            return;
        }

        await ListenAndSendAsync(session, transfer, info, cancellationToken).ConfigureAwait(false);
    }

    public void Accept(DccTransfer transfer, string? savePath = null)
    {
        if (transfer.Session is not IrcSession session || transfer.Offer is null || !transfer.CanAccept)
        {
            return;
        }

        savePath ??= UniquePath(Path.Combine(FolderFor(session.Settings), transfer.FileName));
        transfer.FilePath = savePath;
        transfer.Status = DccStatus.Connecting;
        _ = ReceiveAsync(session, transfer);
    }

    public void Decline(DccTransfer transfer)
    {
        transfer.Status = DccStatus.Declined;
        transfer.Detail = "You declined this file.";
        transfer.Cts.Cancel();
    }

    public void Cancel(DccTransfer transfer)
    {
        transfer.Cts.Cancel();
        if (!transfer.IsFinished)
        {
            transfer.Status = DccStatus.Cancelled;
            transfer.Detail = "Cancelled.";
        }
    }

    public void Dispose()
    {
        foreach (var transfer in Transfers.ToArray())
        {
            Cancel(transfer);
        }
    }

    private async Task FallbackClassicIfNeededAsync(IrcSession session, DccTransfer transfer, FileInfo info)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(40), transfer.Cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (transfer.Status is not DccStatus.Waiting)
        {
            return;
        }

        transfer.IsReverse = false;
        transfer.Detail = "They did not open a port. Waiting for them to connect to you instead.";
        try
        {
            await ListenAndSendAsync(session, transfer, info, transfer.Cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Fail(transfer, ex.Message);
        }
    }

    private async Task ListenAndSendAsync(IrcSession session, DccTransfer transfer, FileInfo info, CancellationToken cancellationToken)
    {
        var local = session.LocalAddress;
        if (local is null || local.AddressFamily != AddressFamily.InterNetwork)
        {
            Fail(transfer, "Need an IPv4 IRC connection to advertise a file send.");
            return;
        }

        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        transfer.Status = DccStatus.Waiting;
        transfer.Detail = "Waiting for " + transfer.PeerNick + " to connect to port " + port + ".";
        await session.CtcpRequestAsync(transfer.PeerNick,
                DccParser.FormatSend(transfer.FileName, DccParser.ToIrcIPv4(local), port, info.Length, transfer.Token),
                cancellationToken)
            .ConfigureAwait(false);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        try
        {
            using var client = await listener.AcceptTcpClientAsync(timeout.Token).ConfigureAwait(false);
            await using var stream = client.GetStream();
            await PumpFileAsync(transfer, info, stream, sending: true, timeout.Token).ConfigureAwait(false);
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task SendOnConnectedAsync(IrcSession session, DccTransfer transfer, DccOffer target)
    {
        try
        {
            transfer.Status = DccStatus.Connecting;
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(transfer.Cts.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await client.ConnectAsync(IPAddress.Parse(target.Address), target.Port, timeout.Token).ConfigureAwait(false);
            var info = new FileInfo(transfer.FilePath);
            await using var stream = client.GetStream();
            await PumpFileAsync(transfer, info, stream, sending: true, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Fail(transfer, ex.Message);
        }
    }

    private async Task ReceiveAsync(IrcSession session, DccTransfer transfer)
    {
        try
        {
            var offer = transfer.Offer!;
            if (offer.IsReverse)
            {
                await ReceiveReverseAsync(session, transfer, offer).ConfigureAwait(false);
                return;
            }

            transfer.Status = DccStatus.Connecting;
            transfer.Detail = "Connecting to " + transfer.PeerNick + "…";
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(transfer.Cts.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await client.ConnectAsync(IPAddress.Parse(offer.Address), offer.Port, timeout.Token).ConfigureAwait(false);
            await using var stream = client.GetStream();
            await PumpFileAsync(transfer, new FileInfo(transfer.FilePath), stream, sending: false, timeout.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Fail(transfer, ex.Message);
        }
    }

    private async Task ReceiveReverseAsync(IrcSession session, DccTransfer transfer, DccOffer offer)
    {
        var local = session.LocalAddress;
        if (local is null || local.AddressFamily != AddressFamily.InterNetwork)
        {
            Fail(transfer, "Need IPv4 to receive a reverse send.");
            return;
        }

        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        transfer.Status = DccStatus.Waiting;
        transfer.Detail = "Opened a local port. Telling " + transfer.PeerNick + " to connect.";
        await session.CtcpRequestAsync(transfer.PeerNick,
                DccParser.FormatSend(transfer.FileName, DccParser.ToIrcIPv4(local), port, offer.FileSize, offer.Token),
                transfer.Cts.Token)
            .ConfigureAwait(false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(transfer.Cts.Token);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        TcpClient client;
        try
        {
            client = await listener.AcceptTcpClientAsync(timeout.Token).ConfigureAwait(false);
        }
        finally
        {
            listener.Stop();
        }

        await using var stream = client.GetStream();
        await PumpFileAsync(transfer, new FileInfo(transfer.FilePath), stream, sending: false, timeout.Token)
            .ConfigureAwait(false);
    }

    private async Task PumpFileAsync(DccTransfer transfer, FileInfo info, NetworkStream stream, bool sending, CancellationToken cancellationToken)
    {
        transfer.Status = DccStatus.Transferring;
        var dest = sending ? info.FullName : transfer.FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? _store.TransfersDir);
        var mode = sending ? FileMode.Open : FileMode.Create;
        await using var file = new FileStream(dest, mode, sending ? FileAccess.Read : FileAccess.Write, FileShare.Read, 64 * 1024, true);
        if (sending && info.Length != file.Length)
        {
            // size already set
        }

        var buffer = new byte[64 * 1024];
        var started = DateTime.UtcNow;
        long lastMark = 0;
        var lastAt = started;
        stream.ReadTimeout = 120_000;
        stream.WriteTimeout = 120_000;
        if (sending)
        {
            int read;
            while ((read = await file.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                transfer.Transferred += read;
                UpdateSpeed(transfer, started, ref lastMark, ref lastAt);
            }

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var remaining = transfer.FileSize > 0 ? transfer.FileSize : long.MaxValue;
            while (remaining > 0)
            {
                var toRead = (int)Math.Min(buffer.Length, remaining);
                var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken).ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                transfer.Transferred += read;
                remaining = transfer.FileSize > 0 ? transfer.FileSize - transfer.Transferred : remaining;
                await WriteAckAsync(stream, transfer.Transferred, cancellationToken).ConfigureAwait(false);
                UpdateSpeed(transfer, started, ref lastMark, ref lastAt);
            }

            await file.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!sending && transfer.FileSize > 0 && transfer.Transferred < transfer.FileSize)
        {
            Fail(transfer, "Connection closed before the file finished.");
            return;
        }

        transfer.Transferred = sending ? info.Length : transfer.Transferred;
        transfer.Status = DccStatus.Completed;
        transfer.Detail = sending
            ? "Finished sending to " + transfer.PeerNick + "."
            : "Saved to " + dest;
        transfer.BytesPerSecond = 0;
    }

    private static async Task WriteAckAsync(NetworkStream stream, long transferred, CancellationToken cancellationToken)
    {
        var pos = (uint)(transferred & 0xFFFFFFFF);
        var ack = new byte[]
        {
            (byte)(pos >> 24), (byte)(pos >> 16), (byte)(pos >> 8), (byte)pos
        };
        await stream.WriteAsync(ack, cancellationToken).ConfigureAwait(false);
    }

    private static void UpdateSpeed(DccTransfer transfer, DateTime started, ref long lastMark, ref DateTime lastAt)
    {
        var now = DateTime.UtcNow;
        if ((now - lastAt).TotalMilliseconds < 400)
        {
            return;
        }

        var dt = (now - lastAt).TotalSeconds;
        if (dt > 0)
        {
            transfer.BytesPerSecond = (long)((transfer.Transferred - lastMark) / dt);
        }

        lastMark = transfer.Transferred;
        lastAt = now;
        transfer.Detail = transfer.SpeedLabel + (string.IsNullOrEmpty(transfer.EtaLabel) ? "" : " · " + transfer.EtaLabel);
    }

    private void Fail(DccTransfer transfer, string message)
    {
        if (transfer.IsFinished)
        {
            return;
        }

        transfer.Status = DccStatus.Failed;
        transfer.Detail = message;
    }

    private void Add(DccTransfer transfer)
    {
        Post(() =>
        {
            Transfers.Insert(0, transfer);
        });
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (var i = 2; i < 1000; i++)
        {
            var candidate = Path.Combine(dir, name + " (" + i + ")" + ext);
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(dir, name + "-" + Guid.NewGuid().ToString("N")[..6] + ext);
    }

    private void Post(Action action)
    {
        if (_sync is null || ReferenceEquals(SynchronizationContext.Current, _sync))
        {
            action();
            return;
        }

        _sync.Post(_ => action(), null);
    }
}
