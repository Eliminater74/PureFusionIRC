namespace PureFusionIRC.Core.Buffers;

public enum BufferKind
{
    Server,
    Channel,
    Query,
    Status
}

public enum ChatLineKind
{
    Message,
    Action,
    Notice,
    Join,
    Part,
    Quit,
    Nick,
    Mode,
    Topic,
    Kick,
    Ctcp,
    Info,
    Error,
    Motd,
    Server
}

public sealed class ChatLine
{
    public ChatLine(
        DateTimeOffset timestamp,
        ChatLineKind kind,
        string? nick,
        string text,
        string? target = null,
        bool isSelf = false,
        bool isHighlight = false)
    {
        Timestamp = timestamp;
        Kind = kind;
        Nick = nick;
        Text = text;
        Target = target;
        IsSelf = isSelf;
        IsHighlight = isHighlight;
    }

    public DateTimeOffset Timestamp { get; }
    public ChatLineKind Kind { get; }
    public string? Nick { get; }
    public string Text { get; }
    public string? Target { get; }
    public bool IsSelf { get; }
    public bool IsHighlight { get; }
}

public sealed class NickEntry
{
    public NickEntry(string nick, string prefixes = "")
    {
        Nick = nick;
        Prefixes = prefixes;
    }

    public string Nick { get; set; }
    public string Prefixes { get; set; }
    public string? Account { get; set; }
    public bool Away { get; set; }

    public char HighestPrefix => Prefixes.Length == 0 ? '\0' : Prefixes[0];

    public string Display => Prefixes + Nick;
}

public enum BufferActivity
{
    None,
    Message,
    Highlight
}
