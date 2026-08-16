namespace PureFusionIRC.Core.Irc;

public sealed class IrcEndpoint
{
    public IrcEndpoint(string host, int port, bool useTls, bool acceptInvalidCertificates = false)
    {
        Host = host;
        Port = port;
        UseTls = useTls;
        AcceptInvalidCertificates = acceptInvalidCertificates;
    }

    public string Host { get; }
    public int Port { get; }
    public bool UseTls { get; }
    public bool AcceptInvalidCertificates { get; }

    public override string ToString() =>
        $"{Host}:{Port}" + (UseTls ? " (TLS)" : string.Empty);

    public static IrcEndpoint Parse(string host, int port = 0, bool? tls = null)
    {
        var useTls = tls ?? port is 6697 or 9999 or 7000;
        if (port <= 0)
        {
            port = useTls ? 6697 : 6667;
        }

        return new IrcEndpoint(host, port, useTls);
    }
}
