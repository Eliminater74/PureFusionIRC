using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace PureFusionIRC.Core.Irc;

/// <summary>Raw TCP or TLS byte stream that speaks CRLF IRC lines.</summary>
public sealed class IrcConnection : IAsyncDisposable
{
    private TcpClient? _tcp;
    private Stream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public bool IsConnected => _tcp?.Connected == true && _stream is not null;

    public async Task ConnectAsync(IrcEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        await DisposeAsync().ConfigureAwait(false);

        var tcp = new TcpClient { NoDelay = true };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        await tcp.ConnectAsync(endpoint.Host, endpoint.Port, timeout.Token).ConfigureAwait(false);

        Stream stream = tcp.GetStream();
        if (endpoint.UseTls)
        {
            var ssl = new SslStream(stream, false, (sender, certificate, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                {
                    return true;
                }

                return endpoint.AcceptInvalidCertificates;
            });

            var options = new SslClientAuthenticationOptions
            {
                TargetHost = endpoint.Host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            };
            await ssl.AuthenticateAsClientAsync(options, timeout.Token).ConfigureAwait(false);
            stream = ssl;
        }

        _tcp = tcp;
        _stream = stream;
        _reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        _writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 4096, leaveOpen: true)
        {
            NewLine = "\r\n",
            AutoFlush = true
        };
    }

    public async Task SendLineAsync(string line, CancellationToken cancellationToken = default)
    {
        if (_writer is null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        if (line.Length > 510)
        {
            line = line[..510];
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_reader is null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (IOException)
            {
                yield break;
            }

            if (line is null)
            {
                yield break;
            }

            yield return line;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _writer?.Dispose();
            _reader?.Dispose();
            if (_stream is not null)
            {
                await _stream.DisposeAsync().ConfigureAwait(false);
            }

            _tcp?.Dispose();
        }
        catch (IOException)
        {
            // Closing a dropped socket is expected.
        }
        finally
        {
            _writer = null;
            _reader = null;
            _stream = null;
            _tcp = null;
        }

        GC.SuppressFinalize(this);
    }
}
