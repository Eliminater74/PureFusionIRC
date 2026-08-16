using PureFusionIRC.Core.Irc;

namespace PureFusionIRC.Core.Tests;

public sealed class ServerFailoverTests
{
    [Fact]
    public void RecordFailure_rotates_after_five_on_multi_server_list()
    {
        var failover = new ServerFailover();
        for (var i = 0; i < 4; i++)
        {
            Assert.False(failover.RecordFailure(3));
            Assert.Equal(0, failover.Index);
        }

        Assert.True(failover.RecordFailure(3));
        Assert.Equal(1, failover.Index);
        Assert.Equal(0, failover.FailStreak);
    }

    [Fact]
    public void RecordFailure_wraps_and_does_not_rotate_single_server()
    {
        var failover = new ServerFailover();
        failover.RecordFailure(2);
        failover.RecordFailure(2);
        failover.RecordFailure(2);
        failover.RecordFailure(2);
        failover.RecordFailure(2);
        Assert.Equal(1, failover.Index);
        failover.RecordFailure(2);
        failover.RecordFailure(2);
        failover.RecordFailure(2);
        failover.RecordFailure(2);
        Assert.True(failover.RecordFailure(2));
        Assert.Equal(0, failover.Index);

        var single = new ServerFailover();
        for (var i = 0; i < 12; i++)
        {
            Assert.False(single.RecordFailure(1));
        }

        Assert.Equal(0, single.Index);
    }

    [Fact]
    public void RecordSuccess_resets_streak()
    {
        var failover = new ServerFailover();
        failover.RecordFailure(2);
        failover.RecordFailure(2);
        failover.RecordSuccess();
        Assert.Equal(0, failover.FailStreak);
        Assert.False(failover.RecordFailure(2));
        Assert.Equal(0, failover.Index);
    }
}
