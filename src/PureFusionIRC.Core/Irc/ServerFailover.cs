namespace PureFusionIRC.Core.Irc;

/// <summary>After several failed connects, walk to the next ServerEntry on this network.</summary>
public sealed class ServerFailover
{
    public const int FailuresBeforeNext = 5;

    public int Index { get; private set; }
    public int FailStreak { get; private set; }

    public void EnsureIndex(int serverCount)
    {
        if (serverCount <= 0)
        {
            Index = 0;
            return;
        }

        Index %= serverCount;
        if (Index < 0)
        {
            Index += serverCount;
        }
    }

    public void RecordSuccess() => FailStreak = 0;

    /// <summary>Returns true when the active server index advanced.</summary>
    public bool RecordFailure(int serverCount)
    {
        EnsureIndex(serverCount);
        FailStreak++;
        if (FailStreak < FailuresBeforeNext)
        {
            return false;
        }

        FailStreak = 0;
        if (serverCount <= 1)
        {
            return false;
        }

        Index = (Index + 1) % serverCount;
        return true;
    }
}
