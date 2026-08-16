using PureFusionIRC.Core.Models;

namespace PureFusionIRC.Core.Settings;

/// <summary>
/// Built-in networks grouped by country. IRCnet (USA) uses the SASL TLS host from ircnet.chat
/// plus the US round-robin. Regional IRCnet profiles keep country-local fallbacks.
/// </summary>
public static class DefaultNetworks
{
    public const int Revision = 2;

    public static readonly IReadOnlyList<string> CountryOrder =
    [
        "United States",
        "Global",
        "Germany",
        "United Kingdom",
        "France",
        "Netherlands",
        "Finland",
        "Canada",
        "Australia",
        "Poland",
        "Japan"
    ];

    public static List<NetworkProfile> Create() =>
    [
        // United States
        Net("IRCnet (USA)", "United States",
            "TLS + SASL entry from ircnet.chat (sasl.irc.atw.hu). Also the US round-robin.",
            true,
            S("sasl.irc.atw.hu", 6697, tls: true, invalidCert: true),
            S("irc.us.ircnet.net", 6697, tls: true, invalidCert: true)),
        Net("EFnet (USA)", "United States", "US-heavy EFnet entry; Choopa is New Jersey.", true,
            S("irc.choopa.net", 9999, tls: true, invalidCert: true),
            S("irc.efnet.org", 6697, tls: true, invalidCert: true)),
        Net("DALnet (USA)", "United States", null, true,
            S("us.dal.net", 6697, tls: true, invalidCert: true),
            S("irc.dal.net", 6697, tls: true, invalidCert: true)),
        Net("Undernet (USA)", "United States", "US round-robin. Many Undernet hubs are still plaintext.", true,
            S("us.undernet.org", 6667, tls: false)),
        Net("GameSurge", "United States", "Gaming network, US-centric.", true,
            S("irc.gamesurge.net", 6667, tls: false)),
        Net("EsperNet", "United States", null, true,
            S("irc.esper.net", 6697)),
        Net("Snoonet", "United States", "Reddit-oriented, US hosted.", true,
            S("irc.snoonet.org", 6697)),
        Net("SlashNET", "United States", null, true,
            S("irc.slashnet.org", 6697, tls: true, invalidCert: true)),
        Net("AfterNET", "United States", null, true,
            S("irc.afternet.org", 6697)),

        // Global anycast / international
        Net("Libera Chat", "Global", "General-purpose FOSS network (anycast, including US/EU).", true,
            S("irc.libera.chat", 6697)),
        Net("OFTC", "Global", null, true,
            S("irc.oftc.net", 6697)),
        Net("Rizon", "Global", null, true,
            S("irc.rizon.net", 6697)),
        Net("QuakeNet", "Global", "Large EU gaming network, global round-robin.", true,
            S("irc.quakenet.org", 6697, tls: true, invalidCert: true)),
        Net("IRCnet (International)", "Global", "Generic IRCnet round-robin if you are not picking a country.", true,
            S("open.ircnet.net", 6667, tls: false)),

        // Germany
        Net("IRCnet (Germany)", "Germany", "FU Berlin and Erlangen public IRCnet servers.", true,
            S("irc.fu-berlin.de", 6697, tls: true, invalidCert: true),
            S("irc.uni-erlangen.de", 6697, tls: true, invalidCert: true),
            S("irc.fu-berlin.de", 6667, tls: false)),
        Net("hackint", "Germany", "German FOSS/hackerspace network.", true,
            S("irc.hackint.org", 6697)),
        Net("EUIrc", "Germany", "German-language network.", true,
            S("irc.euirc.net", 6667, tls: false)),
        Net("German-Elite", "Germany", null, true,
            S("irc.german-elite.net", 6667, tls: false)),

        // United Kingdom
        Net("IRCnet (UK)", "United Kingdom", null, true,
            S("irc.uk.ircnet.net", 6667, tls: false),
            S("open.ircnet.net", 6667, tls: false)),
        Net("Libera Chat (EU)", "United Kingdom", "Same Libera anycast; listed here for EU users.", true,
            S("irc.libera.chat", 6697)),

        // France
        Net("IRCnet (France)", "France", null, true,
            S("irc.fr.ircnet.net", 6667, tls: false),
            S("open.ircnet.net", 6667, tls: false)),

        // Netherlands
        Net("IRCnet (Netherlands)", "Netherlands", null, true,
            S("irc.nl.ircnet.net", 6667, tls: false),
            S("open.ircnet.net", 6667, tls: false)),
        Net("OFTC (EU)", "Netherlands", null, true,
            S("irc.oftc.net", 6697)),

        // Finland (IRCnet origin)
        Net("IRCnet (Finland)", "Finland", "IRCnet started in Finland; open round-robin.", true,
            S("open.ircnet.net", 6667, tls: false)),

        // Canada
        Net("IRCnet (Canada)", "Canada", "Uses the SASL TLS host (same as USA) plus global fallback.", true,
            S("sasl.irc.atw.hu", 6697, tls: true, invalidCert: true),
            S("open.ircnet.net", 6667, tls: false)),
        Net("Libera Chat (Canada)", "Canada", null, true,
            S("irc.libera.chat", 6697)),

        // Australia
        Net("AustNet", "Australia", null, true,
            S("irc.austnet.org", 6667, tls: false)),
        Net("OzOrg", "Australia", null, true,
            S("irc.oz.org", 6667, tls: false)),

        // Poland
        Net("pirc.pl", "Poland", null, true,
            S("irc.pirc.pl", 6697)),

        // Japan
        Net("Rizon (Japan)", "Japan", "Rizon anycast; popular with JP users.", true,
            S("irc.rizon.net", 6697))
    ];

    public static void MergeInto(List<NetworkProfile> existing)
    {
        UpgradeLegacyIrcnet(existing);
        var names = new HashSet<string>(existing.Select(n => n.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var builtin in Create())
        {
            if (names.Add(builtin.Name))
            {
                existing.Add(builtin);
            }
            else
            {
                var match = existing.First(n => string.Equals(n.Name, builtin.Name, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(match.Country))
                {
                    match.Country = builtin.Country;
                }
            }
        }
    }

    public static IEnumerable<IGrouping<string, NetworkProfile>> GroupByCountry(IEnumerable<NetworkProfile> networks)
    {
        int Rank(string country)
        {
            var index = CountryOrder.ToList().IndexOf(country);
            return index < 0 ? 1000 + country.GetHashCode() : index;
        }

        return networks
            .GroupBy(n => string.IsNullOrWhiteSpace(n.Country) ? "Global" : n.Country)
            .OrderBy(g => Rank(g.Key))
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
    }

    private static void UpgradeLegacyIrcnet(List<NetworkProfile> existing)
    {
        var legacy = existing.FirstOrDefault(n =>
            n.Name.Equals("IRCnet", StringComparison.OrdinalIgnoreCase));
        if (legacy is null)
        {
            return;
        }

        legacy.Name = "IRCnet (USA)";
        legacy.Country = "United States";
        legacy.Comment = "TLS + SASL entry from ircnet.chat (sasl.irc.atw.hu).";
        legacy.Servers =
        [
            S("sasl.irc.atw.hu", 6697, tls: true, invalidCert: true),
            S("irc.us.ircnet.net", 6697, tls: true, invalidCert: true)
        ];
    }

    private static NetworkProfile Net(string name, string country, string? comment, bool enabled, params ServerEntry[] servers) =>
        new()
        {
            Name = name,
            Country = country,
            Comment = comment,
            Enabled = enabled,
            Servers = servers.ToList()
        };

    private static ServerEntry S(string host, int port, bool tls = true, bool invalidCert = false) =>
        new()
        {
            Host = host,
            Port = port,
            UseTls = tls,
            AcceptInvalidCertificates = invalidCert
        };
}
