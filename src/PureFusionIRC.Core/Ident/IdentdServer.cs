using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PureFusionIRC.Core.Ident;

/// <summary>
/// RFC 1413 identd on TCP 113 so networks that still fingerprint ident (IRCnet, some EFnet) can finish login.
/// Binding 113 on Windows usually needs Administrator; bind failures are reported, never crash chat.
/// </summary>
public sealed class IdentdServer : IDisposable
{
    private readonly Func<string> _username;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _accept;

    public IdentdServer(Func<string> username)
    {
        _username = username;
    }

    public bool IsRunning => _listener is not null;
    public int Port { get; private set; } = 113;
    public string? LastError { get; private set; }

    public string? Start(IPAddress? address = null, int port = 113)
    {
        if (IsRunning)
        {
            return null;
        }

        LastError = null;
        Port = port;
        try
        {
            var listener = new TcpListener(address ?? IPAddress.Any, port);
            listener.Start();
            _listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _cts = new CancellationTokenSource();
            _accept = Task.Run(() => AcceptLoopAsync(_cts.Token));
            return null;
        }
        catch (SocketException ex)
        {
            LastError = port == 113
                ? "Identd could not bind TCP 113 (" + ex.SocketErrorCode + "). Run as Administrator if this network waits on ident."
                : "Identd could not bind port " + port + ": " + ex.Message;
            return LastError;
        }
        catch (Exception ex)
        {
            LastError = "Identd failed: " + ex.Message;
            return LastError;
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        try
        {
            _listener?.Stop();
        }
        catch (ObjectDisposedException)
        {
        }

        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException or SocketException)
            {
                return;
            }

            _ = Task.Run(() => HandleClientAsync(client), CancellationToken.None);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 8000;
                client.SendTimeout = 8000;
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 128, leaveOpen: true);
                await using var writer = new StreamWriter(stream, Encoding.ASCII, 128, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                {
                    return;
                }

                string reply;
                if (IdentdProtocol.TryParseQuery(line, out var localPort, out var remotePort))
                {
                    reply = IdentdProtocol.FormatUserId(localPort, remotePort, _username());
                }
                else
                {
                    reply = IdentdProtocol.FormatError(0, 0);
                }

                await writer.WriteLineAsync(reply).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ident must never take down IRC.
            }
        }
    }
}
