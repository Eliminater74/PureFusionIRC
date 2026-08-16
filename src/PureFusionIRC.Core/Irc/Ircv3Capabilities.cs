namespace PureFusionIRC.Core.Irc;

/// <summary>
/// Tracks IRCv3 CAP LS/ACK including multiline <c>CAP * LS *</c> lists so we do not REQ too early.
/// </summary>
public sealed class Ircv3Capabilities
{
    public static readonly string[] AlwaysWant =
    [
        "multi-prefix",
        "server-time",
        "account-tag",
        "extended-join",
        "away-notify",
        "chghost",
        "message-tags",
        "userhost-in-names",
        "echo-message",
        "account-notify",
        "invite-notify",
        "cap-notify",
        "labeled-response",
        "batch",
        "setname",
        "chathistory",
        "draft/chathistory",
        "draft/multiline",
        "multiline",
        "standard-replies"
    ];

    private readonly HashSet<string> _offered = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _enabled = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ackBuffer = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Enabled => _enabled;

    public bool Has(string name) => _enabled.Contains(name);

    public void Reset()
    {
        _offered.Clear();
        _enabled.Clear();
        _ackBuffer.Clear();
    }

    public static string Subcommand(IrcMessage message) =>
        message.Parameters.Count >= 2 ? message.Parameters[1] : message.Trailing ?? string.Empty;

    public static bool IsContinued(IrcMessage message) =>
        message.Parameters.Count >= 3 && message.Parameters[2] == "*";

    public static IReadOnlyList<string> Tokens(IrcMessage message) =>
        (message.Trailing ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public static string CapName(string token)
    {
        var eq = token.IndexOf('=');
        return eq < 0 ? token : token[..eq];
    }

    /// <summary>Returns capabilities to REQ, or empty while a multiline LS is still coming.</summary>
    public IReadOnlyList<string> NoteLs(IrcMessage message, bool wantSasl)
    {
        RememberOffered(message);
        if (IsContinued(message))
        {
            return Array.Empty<string>();
        }

        return SelectWanted(wantSasl);
    }

    public IReadOnlyList<string> NoteNew(IrcMessage message, bool wantSasl)
    {
        RememberOffered(message);
        return SelectWanted(wantSasl).Where(name => !_enabled.Contains(name)).ToList();
    }

    /// <returns>true when ACK is complete (not a continuation line).</returns>
    public bool NoteAck(IrcMessage message)
    {
        foreach (var token in Tokens(message))
        {
            _ackBuffer.Add(CapName(token));
        }

        if (IsContinued(message))
        {
            return false;
        }

        foreach (var name in _ackBuffer)
        {
            _enabled.Add(name);
        }

        _ackBuffer.Clear();
        return true;
    }

    public void NoteDel(IrcMessage message)
    {
        foreach (var token in Tokens(message))
        {
            _enabled.Remove(CapName(token));
        }
    }

    public static string ChannelTarget(IrcMessage message)
    {
        var first = message[0];
        if (LooksLikeChannel(first))
        {
            return first!;
        }

        return message.Trailing ?? first ?? string.Empty;
    }

    public static bool LooksLikeChannel(string? target) =>
        !string.IsNullOrEmpty(target) && target[0] is '#' or '&' or '+' or '!';

    public static string BareNick(string token)
    {
        var bang = token.IndexOf('!');
        return bang < 0 ? token : token[..bang];
    }

    private void RememberOffered(IrcMessage message)
    {
        foreach (var token in Tokens(message))
        {
            _offered.Add(CapName(token));
        }
    }

    private List<string> SelectWanted(bool wantSasl)
    {
        var want = new List<string>();
        foreach (var name in AlwaysWant)
        {
            if (_offered.Contains(name))
            {
                want.Add(name);
            }
        }

        if (wantSasl && _offered.Contains("sasl") && !want.Contains("sasl", StringComparer.OrdinalIgnoreCase))
        {
            want.Add("sasl");
        }

        return want;
    }
}
