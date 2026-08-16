using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        bool isHighlight = false,
        string? messageId = null,
        string? replyId = null)
    {
        Timestamp = timestamp;
        Kind = kind;
        Nick = nick;
        Text = text;
        Target = target;
        IsSelf = isSelf;
        IsHighlight = isHighlight;
        MessageId = messageId;
        ReplyId = replyId;
    }

    public DateTimeOffset Timestamp { get; }
    public ChatLineKind Kind { get; }
    public string? Nick { get; }
    public string Text { get; }
    public string? Target { get; }
    public bool IsSelf { get; }
    public bool IsHighlight { get; }
    public string? MessageId { get; }
    public string? ReplyId { get; }
}

public sealed class NickEntry : INotifyPropertyChanged
{
    private string _nick;
    private string _prefixes;
    private string? _account;
    private bool _away;
    private bool _isSelf;
    private int? _idleSeconds;

    public NickEntry(string nick, string prefixes = "")
    {
        _nick = nick;
        _prefixes = NormalizePrefixes(prefixes);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Nick
    {
        get => _nick;
        set => SetField(ref _nick, value);
    }

    public string Prefixes
    {
        get => _prefixes;
        set => SetField(ref _prefixes, NormalizePrefixes(value));
    }

    public string? Account
    {
        get => _account;
        set => SetField(ref _account, value);
    }

    public bool Away
    {
        get => _away;
        set
        {
            if (SetField(ref _away, value))
            {
                OnPropertyChanged(nameof(StatusMarks));
            }
        }
    }

    public bool IsSelf
    {
        get => _isSelf;
        set => SetField(ref _isSelf, value);
    }

    public int? IdleSeconds
    {
        get => _idleSeconds;
        set
        {
            if (SetField(ref _idleSeconds, value))
            {
                OnPropertyChanged(nameof(StatusMarks));
            }
        }
    }

    public char HighestPrefix => Prefixes.Length == 0 ? '\0' : Prefixes[0];

    public string Display => Prefixes + Nick;

    /// <summary>Away = z, idle = i plus a short duration. Rank stays in the colored nick, not here.</summary>
    public string StatusMarks
    {
        get
        {
            var marks = new List<string>();
            if (Away)
            {
                marks.Add("z");
            }

            if (IdleSeconds is int idle && idle >= 60)
            {
                marks.Add(idle >= 3600 ? $"i{idle / 3600}h" : $"i{idle / 60}m");
            }

            return marks.Count == 0 ? string.Empty : " " + string.Join(" ", marks);
        }
    }

    public static string NormalizePrefixes(string prefixes)
    {
        const string order = "~&@%+";
        if (string.IsNullOrEmpty(prefixes))
        {
            return string.Empty;
        }

        return new string(order.Where(prefixes.Contains).ToArray());
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        if (name is nameof(Nick) or nameof(Prefixes))
        {
            OnPropertyChanged(nameof(Display));
            OnPropertyChanged(nameof(HighestPrefix));
        }

        return true;
    }

    private void OnPropertyChanged(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum BufferActivity
{
    None,
    Message,
    Highlight
}
