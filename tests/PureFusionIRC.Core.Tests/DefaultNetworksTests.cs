using PureFusionIRC.Core.Settings;

namespace PureFusionIRC.Core.Tests;

public class DefaultNetworksTests
{
    [Fact]
    public void Ircnet_usa_uses_sasl_host_first()
    {
        var usa = DefaultNetworks.Create().Single(n => n.Name == "IRCnet (USA)");
        Assert.Equal("United States", usa.Country);
        Assert.Equal("sasl.irc.atw.hu", usa.PrimaryServer.Host);
        Assert.Equal(6697, usa.PrimaryServer.Port);
        Assert.True(usa.PrimaryServer.UseTls);
        Assert.Contains(usa.Servers, s => s.Host == "irc.us.ircnet.net");
    }

    [Fact]
    public void Merge_upgrades_legacy_ircnet_name()
    {
        var existing = new List<PureFusionIRC.Core.Models.NetworkProfile>
        {
            new()
            {
                Name = "IRCnet",
                Servers = [new() { Host = "open.ircnet.net", Port = 6667, UseTls = false }]
            }
        };

        DefaultNetworks.MergeInto(existing);
        Assert.Contains(existing, n => n.Name == "IRCnet (USA)" && n.PrimaryServer.Host == "sasl.irc.atw.hu");
        Assert.Contains(existing, n => n.Name == "IRCnet (Germany)");
        Assert.DoesNotContain(existing, n => n.Name == "IRCnet");
    }
}
