using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Irc;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.Core.Commands;

public sealed class CommandContext
{
    public required IrcSession Session { get; init; }
    public required IrcBuffer Buffer { get; init; }
    public required string Raw { get; init; }
    public required string Name { get; init; }
    public required string Arguments { get; init; }

    public string[] Args =>
        string.IsNullOrWhiteSpace(Arguments)
            ? []
            : Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}

public sealed class CommandResult
{
    public bool Handled { get; init; } = true;
    public string? Info { get; init; }
    public string? Error { get; init; }

    public static CommandResult Ok(string? info = null) => new() { Info = info };
    public static CommandResult Fail(string error) => new() { Error = error };
    public static CommandResult Pass => new() { Handled = false };
}

public delegate Task<CommandResult> CommandHandler(CommandContext context, CancellationToken cancellationToken);

public sealed class CommandProcessor
{
    private readonly Dictionary<string, CommandHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    public CommandProcessor()
    {
        RegisterDefaults();
    }

    public IReadOnlyCollection<string> Names => _handlers.Keys;

    public void Register(string name, CommandHandler handler, params string[] aliases)
    {
        _handlers[name] = handler;
        foreach (var alias in aliases)
        {
            _aliases[alias] = name;
        }
    }

    public async Task<CommandResult> ExecuteAsync(IrcSession session, IrcBuffer buffer, string raw, CancellationToken cancellationToken = default)
    {
        if (raw.StartsWith("//", StringComparison.Ordinal))
        {
            await session.PrivmsgAsync(buffer.Name, raw[1..], cancellationToken).ConfigureAwait(false);
            return CommandResult.Ok();
        }

        if (!raw.StartsWith('/'))
        {
            if (buffer.Kind is BufferKind.Channel or BufferKind.Query)
            {
                await session.PrivmsgAsync(buffer.Name, raw, cancellationToken).ConfigureAwait(false);
                return CommandResult.Ok();
            }

            return CommandResult.Fail("Not on a channel. Use /join #channel or /query nick.");
        }

        var remainder = raw[1..];
        var space = remainder.IndexOf(' ');
        var name = space < 0 ? remainder : remainder[..space];
        var args = space < 0 ? string.Empty : remainder[(space + 1)..];
        if (_aliases.TryGetValue(name, out var canonical))
        {
            name = canonical;
        }

        if (!_handlers.TryGetValue(name, out var handler))
        {
            await session.SendRawAsync(remainder, cancellationToken).ConfigureAwait(false);
            return CommandResult.Ok();
        }

        var context = new CommandContext
        {
            Session = session,
            Buffer = buffer,
            Raw = raw,
            Name = name,
            Arguments = args
        };
        return await handler(context, cancellationToken).ConfigureAwait(false);
    }

    private void RegisterDefaults()
    {
        Register("help", HelpAsync, "h", "commands");
        Register("join", JoinAsync, "j");
        Register("part", PartAsync, "leave");
        Register("quit", QuitAsync, "exit");
        Register("disconnect", DisconnectAsync);
        Register("reconnect", ReconnectAsync);
        Register("nick", NickAsync);
        Register("me", MeAsync, "action");
        Register("msg", MsgAsync, "privmsg");
        Register("query", QueryAsync, "q");
        Register("notice", NoticeAsync);
        Register("ctcp", CtcpAsync);
        Register("whois", WhoisAsync);
        Register("whowas", async (c, t) => { await c.Session.SendRawAsync("WHOWAS " + c.Arguments, t); return CommandResult.Ok(); });
        Register("who", async (c, t) => { await c.Session.SendRawAsync("WHO " + (string.IsNullOrWhiteSpace(c.Arguments) ? c.Buffer.Name : c.Arguments), t); return CommandResult.Ok(); });
        Register("mode", ModeAsync);
        Register("topic", TopicAsync);
        Register("kick", KickAsync);
        Register("invite", InviteAsync);
        Register("quote", QuoteAsync, "raw");
        Register("names", async (c, t) => { await c.Session.SendRawAsync("NAMES " + (string.IsNullOrWhiteSpace(c.Arguments) ? c.Buffer.Name : c.Arguments), t); return CommandResult.Ok(); });
        Register("list", async (c, t) => { await c.Session.SendRawAsync("LIST " + c.Arguments, t); return CommandResult.Ok(); });
        Register("away", async (c, t) => { await c.Session.SendRawAsync(string.IsNullOrWhiteSpace(c.Arguments) ? "AWAY" : "AWAY :" + c.Arguments, t); return CommandResult.Ok(); });
        Register("back", async (c, t) => { await c.Session.SendRawAsync("AWAY", t); return CommandResult.Ok(); });
        Register("ping", PingAsync);
        Register("clear", (c, _) => { c.Buffer.Clear(); return Task.FromResult(CommandResult.Ok()); });
        Register("theme", ThemeAsync);
        Register("echo", (c, _) => { c.Session.Print(c.Buffer, ChatLineKind.Info, c.Arguments); return Task.FromResult(CommandResult.Ok()); });
        Register("say", async (c, t) => { await c.Session.PrivmsgAsync(c.Buffer.Name, c.Arguments, t); return CommandResult.Ok(); });
        Register("hop", HopAsync, "cycle");
        Register("umode", async (c, t) => { await c.Session.SendRawAsync("MODE " + c.Session.CurrentNick + " " + c.Arguments, t); return CommandResult.Ok(); });
        Register("motd", async (c, t) => { await c.Session.SendRawAsync("MOTD", t); return CommandResult.Ok(); });
        Register("lusers", async (c, t) => { await c.Session.SendRawAsync("LUSERS", t); return CommandResult.Ok(); });
        Register("links", async (c, t) => { await c.Session.SendRawAsync("LINKS " + c.Arguments, t); return CommandResult.Ok(); });
        Register("time", async (c, t) => { await c.Session.SendRawAsync("TIME", t); return CommandResult.Ok(); });
        Register("version", async (c, t) => { await c.Session.SendRawAsync("VERSION " + c.Arguments, t); return CommandResult.Ok(); });
        Register("admin", async (c, t) => { await c.Session.SendRawAsync("ADMIN " + c.Arguments, t); return CommandResult.Ok(); });
        Register("info", async (c, t) => { await c.Session.SendRawAsync("INFO", t); return CommandResult.Ok(); });
        Register("stats", async (c, t) => { await c.Session.SendRawAsync("STATS " + c.Arguments, t); return CommandResult.Ok(); });
        Register("dcc", (_, _) => Task.FromResult(CommandResult.Fail("DCC is not implemented yet. See TODO.md.")));
        Register("server", (_, _) => Task.FromResult(CommandResult.Fail("Use the Networks window (File → Networks) in this version.")));
    }

    private static Task<CommandResult> HelpAsync(CommandContext c, CancellationToken _)
    {
        var names = string.Join(", ", c.Session.Commands.Names.OrderBy(n => n));
        c.Session.Print(c.Buffer, ChatLineKind.Info, "Commands: " + names);
        return Task.FromResult(CommandResult.Ok());
    }

    private static async Task<CommandResult> JoinAsync(CommandContext c, CancellationToken t)
    {
        if (string.IsNullOrWhiteSpace(c.Arguments))
        {
            return CommandResult.Fail("Usage: /join #channel [key]");
        }

        await c.Session.SendRawAsync("JOIN " + c.Arguments, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> PartAsync(CommandContext c, CancellationToken t)
    {
        var target = c.Buffer.Kind == BufferKind.Channel ? c.Buffer.Name : c.Args.FirstOrDefault();
        if (string.IsNullOrEmpty(target))
        {
            return CommandResult.Fail("Usage: /part [#channel] [reason]");
        }

        var reasonIndex = c.Arguments.StartsWith(target, StringComparison.OrdinalIgnoreCase)
            ? c.Arguments.IndexOf(' ')
            : -1;
        var reason = reasonIndex > 0 ? c.Arguments[(reasonIndex + 1)..] : (c.Buffer.Kind == BufferKind.Channel ? c.Arguments : string.Empty);
        await c.Session.SendRawAsync(string.IsNullOrWhiteSpace(reason) ? "PART " + target : "PART " + target + " :" + reason, t)
            .ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> QuitAsync(CommandContext c, CancellationToken t)
    {
        await c.Session.QuitAsync(string.IsNullOrWhiteSpace(c.Arguments) ? "PureFusionIRC" : c.Arguments, t)
            .ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> DisconnectAsync(CommandContext c, CancellationToken t)
    {
        await c.Session.DisconnectAsync(t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> ReconnectAsync(CommandContext c, CancellationToken t)
    {
        await c.Session.ReconnectAsync(t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> NickAsync(CommandContext c, CancellationToken t)
    {
        if (string.IsNullOrWhiteSpace(c.Arguments))
        {
            return CommandResult.Fail("Usage: /nick newnick");
        }

        await c.Session.SendRawAsync("NICK " + c.Args[0], t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> MeAsync(CommandContext c, CancellationToken t)
    {
        if (c.Buffer.Kind is not (BufferKind.Channel or BufferKind.Query))
        {
            return CommandResult.Fail("Actions only work in a channel or query.");
        }

        await c.Session.ActionAsync(c.Buffer.Name, c.Arguments, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> MsgAsync(CommandContext c, CancellationToken t)
    {
        if (c.Args.Length < 2)
        {
            return CommandResult.Fail("Usage: /msg <target> <text>");
        }

        var target = c.Args[0];
        var text = c.Arguments[(target.Length)..].TrimStart();
        await c.Session.PrivmsgAsync(target, text, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> QueryAsync(CommandContext c, CancellationToken t)
    {
        if (c.Args.Length == 0)
        {
            return CommandResult.Fail("Usage: /query <nick> [text]");
        }

        var nick = c.Args[0];
        c.Session.OpenQuery(nick);
        if (c.Args.Length > 1)
        {
            var text = c.Arguments[(nick.Length)..].TrimStart();
            await c.Session.PrivmsgAsync(nick, text, t).ConfigureAwait(false);
        }

        return CommandResult.Ok();
    }

    private static async Task<CommandResult> NoticeAsync(CommandContext c, CancellationToken t)
    {
        if (c.Args.Length < 2)
        {
            return CommandResult.Fail("Usage: /notice <target> <text>");
        }

        var target = c.Args[0];
        var text = c.Arguments[(target.Length)..].TrimStart();
        await c.Session.SendRawAsync("NOTICE " + target + " :" + text, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> CtcpAsync(CommandContext c, CancellationToken t)
    {
        if (c.Args.Length < 2)
        {
            return CommandResult.Fail("Usage: /ctcp <nick> <command> [args]");
        }

        var nick = c.Args[0];
        var rest = c.Arguments[(nick.Length)..].TrimStart();
        await c.Session.CtcpRequestAsync(nick, rest, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> WhoisAsync(CommandContext c, CancellationToken t)
    {
        var nick = string.IsNullOrWhiteSpace(c.Arguments) ? c.Buffer.Name : c.Args[0];
        await c.Session.SendRawAsync("WHOIS " + nick, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> ModeAsync(CommandContext c, CancellationToken t)
    {
        var target = c.Args.Length == 0
            ? (c.Buffer.Kind == BufferKind.Channel ? c.Buffer.Name : c.Session.CurrentNick)
            : c.Arguments;
        await c.Session.SendRawAsync(c.Args.Length == 0 ? "MODE " + target : "MODE " + target, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> TopicAsync(CommandContext c, CancellationToken t)
    {
        var channel = c.Buffer.Kind == BufferKind.Channel ? c.Buffer.Name : c.Args.FirstOrDefault();
        if (string.IsNullOrEmpty(channel))
        {
            return CommandResult.Fail("Usage: /topic [#channel] [new topic]");
        }

        if (c.Buffer.Kind == BufferKind.Channel && !string.IsNullOrWhiteSpace(c.Arguments))
        {
            await c.Session.SendRawAsync("TOPIC " + channel + " :" + c.Arguments, t).ConfigureAwait(false);
        }
        else if (c.Args.Length >= 2)
        {
            var rest = c.Arguments[(channel.Length)..].TrimStart();
            await c.Session.SendRawAsync("TOPIC " + channel + " :" + rest, t).ConfigureAwait(false);
        }
        else
        {
            await c.Session.SendRawAsync("TOPIC " + channel, t).ConfigureAwait(false);
        }

        return CommandResult.Ok();
    }

    private static async Task<CommandResult> KickAsync(CommandContext c, CancellationToken t)
    {
        if (c.Args.Length == 0)
        {
            return CommandResult.Fail("Usage: /kick <nick> [reason]");
        }

        var channel = c.Buffer.Kind == BufferKind.Channel ? c.Buffer.Name : null;
        var nick = c.Args[0];
        var reason = c.Arguments[(nick.Length)..].TrimStart();
        if (channel is null)
        {
            return CommandResult.Fail("Kick from a channel buffer.");
        }

        await c.Session.SendRawAsync(
            string.IsNullOrEmpty(reason) ? $"KICK {channel} {nick}" : $"KICK {channel} {nick} :{reason}",
            t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> InviteAsync(CommandContext c, CancellationToken t)
    {
        if (c.Args.Length == 0)
        {
            return CommandResult.Fail("Usage: /invite <nick> [#channel]");
        }

        var nick = c.Args[0];
        var channel = c.Args.Length > 1 ? c.Args[1] : c.Buffer.Name;
        await c.Session.SendRawAsync($"INVITE {nick} {channel}", t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> QuoteAsync(CommandContext c, CancellationToken t)
    {
        if (string.IsNullOrWhiteSpace(c.Arguments))
        {
            return CommandResult.Fail("Usage: /quote <raw IRC>");
        }

        await c.Session.SendRawAsync(c.Arguments, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static async Task<CommandResult> PingAsync(CommandContext c, CancellationToken t)
    {
        if (c.Args.Length == 0)
        {
            await c.Session.SendLagPingAsync(t).ConfigureAwait(false);
            return CommandResult.Ok();
        }

        await c.Session.CtcpRequestAsync(c.Args[0], "PING " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), t)
            .ConfigureAwait(false);
        return CommandResult.Ok();
    }

    private static Task<CommandResult> ThemeAsync(CommandContext c, CancellationToken _)
    {
        if (string.IsNullOrWhiteSpace(c.Arguments))
        {
            c.Session.Print(c.Buffer, ChatLineKind.Info, "Current theme: " + c.Session.Theme.Id + " — /theme <id>");
            return Task.FromResult(CommandResult.Ok());
        }

        c.Session.RequestTheme(c.Arguments.Trim());
        return Task.FromResult(CommandResult.Ok());
    }

    private static async Task<CommandResult> HopAsync(CommandContext c, CancellationToken t)
    {
        if (c.Buffer.Kind != BufferKind.Channel)
        {
            return CommandResult.Fail("/hop only works on a channel.");
        }

        await c.Session.SendRawAsync("PART " + c.Buffer.Name, t).ConfigureAwait(false);
        await c.Session.SendRawAsync("JOIN " + c.Buffer.Name, t).ConfigureAwait(false);
        return CommandResult.Ok();
    }
}
