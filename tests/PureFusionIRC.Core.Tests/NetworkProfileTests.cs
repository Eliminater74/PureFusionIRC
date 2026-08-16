using PureFusionIRC.Core.Models;

namespace PureFusionIRC.Core.Tests;

public sealed class NetworkProfileTests
{
    [Fact]
    public void SetAutoJoin_adds_hash_and_skips_duplicates()
    {
        var network = new NetworkProfile();
        Assert.True(network.SetAutoJoin("lounge", true));
        Assert.True(network.HasAutoJoin("#lounge"));
        Assert.True(network.HasAutoJoin("Lounge"));
        Assert.True(network.SetAutoJoin("#LOUNGE", true));
        Assert.Single(network.AutoJoin);
        Assert.Equal("#lounge", network.AutoJoin[0]);
    }

    [Fact]
    public void SetAutoJoin_keeps_channel_key_when_already_listed()
    {
        var network = new NetworkProfile { AutoJoin = ["#secret hunter2"] };
        Assert.True(network.HasAutoJoin("#secret"));
        network.SetAutoJoin("#secret", true);
        Assert.Equal(["#secret hunter2"], network.AutoJoin);
        Assert.False(network.SetAutoJoin("#secret", false));
        Assert.Empty(network.AutoJoin);
    }
}
