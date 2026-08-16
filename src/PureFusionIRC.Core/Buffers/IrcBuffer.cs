using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PureFusionIRC.Core.Buffers;

public sealed class IrcBuffer : INotifyPropertyChanged
{
    private string? _topic;
    private BufferActivity _activity;
    private int _userCount;

    public IrcBuffer(string sessionId, BufferKind kind, string name)
    {
        SessionId = sessionId;
        Kind = kind;
        Name = name;
        Key = sessionId + "|" + kind + "|" + name.ToLowerInvariant();
    }

    public string SessionId { get; }
    public BufferKind Kind { get; }
    public string Name { get; }
    public string Key { get; }
    public ObservableCollection<ChatLine> Lines { get; } = new();
    public ObservableCollection<NickEntry> Nicks { get; } = new();

    public string? Topic
    {
        get => _topic;
        set => SetField(ref _topic, value);
    }

    public BufferActivity Activity
    {
        get => _activity;
        set => SetField(ref _activity, value);
    }

    public int UserCount
    {
        get => _userCount;
        set => SetField(ref _userCount, value);
    }

    public Dictionary<string, NickEntry> NickMap { get; } = new(StringComparer.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Append(ChatLine line, int maxLines)
    {
        Lines.Add(line);
        while (Lines.Count > maxLines)
        {
            Lines.RemoveAt(0);
        }
    }

    public void Clear() => Lines.Clear();

    public void ReplaceNicks(IEnumerable<NickEntry> nicks)
    {
        NickMap.Clear();
        Nicks.Clear();
        foreach (var nick in nicks.OrderBy(NickSort))
        {
            NickMap[nick.Nick] = nick;
            Nicks.Add(nick);
        }

        UserCount = Nicks.Count;
    }

    public void UpsertNick(NickEntry entry)
    {
        if (NickMap.TryGetValue(entry.Nick, out var existing))
        {
            if (!string.IsNullOrEmpty(entry.Prefixes))
            {
                existing.Prefixes = entry.Prefixes;
            }

            existing.Account = entry.Account ?? existing.Account;
            existing.IsSelf = entry.IsSelf;
        }
        else
        {
            NickMap[entry.Nick] = entry;
        }

        RebuildNickOrder();
    }

    public void ApplyPresence(string nick, bool? away = null, int? idleSeconds = null, string? prefixes = null)
    {
        if (!NickMap.TryGetValue(nick, out var existing))
        {
            return;
        }

        if (away is not null)
        {
            existing.Away = away.Value;
        }

        if (idleSeconds is not null)
        {
            existing.IdleSeconds = idleSeconds;
        }

        if (!string.IsNullOrEmpty(prefixes))
        {
            existing.Prefixes = prefixes;
        }
    }

    public void RemoveNick(string nick)
    {
        NickMap.Remove(nick);
        RebuildNickOrder();
    }

    public void RenameNick(string oldNick, string newNick)
    {
        if (!NickMap.Remove(oldNick, out var entry))
        {
            return;
        }

        entry.Nick = newNick;
        NickMap[newNick] = entry;
        RebuildNickOrder();
    }

    public void MarkSelf(string selfNick)
    {
        foreach (var entry in NickMap.Values)
        {
            entry.IsSelf = string.Equals(entry.Nick, selfNick, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void AddPrefix(string nick, char prefix)
    {
        if (!NickMap.TryGetValue(nick, out var entry))
        {
            return;
        }

        if (entry.Prefixes.IndexOf(prefix) < 0)
        {
            entry.Prefixes += prefix;
        }

        RebuildNickOrder();
    }

    public void RemovePrefix(string nick, char prefix)
    {
        if (!NickMap.TryGetValue(nick, out var entry))
        {
            return;
        }

        entry.Prefixes = entry.Prefixes.Replace(prefix.ToString(), string.Empty, StringComparison.Ordinal);
        RebuildNickOrder();
    }

    private void RebuildNickOrder()
    {
        var ordered = NickMap.Values.OrderBy(NickSort).ToList();
        Nicks.Clear();
        foreach (var nick in ordered)
        {
            Nicks.Add(nick);
        }

        UserCount = Nicks.Count;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserCount)));
    }

    private static string NickSort(NickEntry entry)
    {
        var rank = entry.HighestPrefix switch
        {
            '~' => "0",
            '&' => "1",
            '@' => "2",
            '%' => "3",
            '+' => "4",
            _ => "9"
        };
        return rank + entry.Nick.ToUpperInvariant();
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
