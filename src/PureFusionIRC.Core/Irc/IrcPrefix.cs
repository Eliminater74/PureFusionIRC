namespace PureFusionIRC.Core.Irc;

/// <summary>Server name or nick!user@host from an incoming line.</summary>
public sealed class IrcPrefix
{
    public IrcPrefix(string raw, string? nick = null, string? user = null, string? host = null)
    {
        Raw = raw;
        Nick = nick;
        User = user;
        Host = host;
    }

    public string Raw { get; }
    public string? Nick { get; }
    public string? User { get; }
    public string? Host { get; }
    public bool IsUser => Nick is not null;

    public static IrcPrefix Parse(string raw)
    {
        var bang = raw.IndexOf('!');
        var at = raw.IndexOf('@');
        if (bang > 0 && at > bang)
        {
            return new IrcPrefix(
                raw,
                raw[..bang],
                raw[(bang + 1)..at],
                raw[(at + 1)..]);
        }

        return new IrcPrefix(raw, nick: bang < 0 && at < 0 ? raw : null);
    }

    public override string ToString() => Raw;
}
