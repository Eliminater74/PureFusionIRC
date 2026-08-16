using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Irc;
using PureFusionIRC.Core.Models;
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
        Register("autojoin", AutoJoinAsync, "ajoin");
        Register("umode", async (c, t) => { await c.Session.SendRawAsync("MODE " + c.Session.CurrentNick + " " + c.Arguments, t); return CommandResult.Ok(); });
        Register("motd", async (c, t) => { await c.Session.SendRawAsync("MOTD", t); return CommandResult.Ok(); });
        Register("lusers", async (c, t) => { await c.Session.SendRawAsync("LUSERS", t); return CommandResult.Ok(); });
        Register("links", async (c, t) => { await c.Session.SendRawAsync("LINKS " + c.Arguments, t); return CommandResult.Ok(); });
        Register("time", async (c, t) => { await c.Session.SendRawAsync("TIME", t); return CommandResult.Ok(); });
        Register("version", async (c, t) => { await c.Session.SendRawAsync("VERSION " + c.Arguments, t); return CommandResult.Ok(); });
        Register("admin", async (c, t) => { await c.Session.SendRawAsync("ADMIN " + c.Arguments, t); return CommandResult.Ok(); });
        Register("info", async (c, t) => { await c.Session.SendRawAsync("INFO", t); return CommandResult.Ok(); });
        Register("stats", async (c, t) => { await c.Session.SendRawAsync("STATS " + c.Arguments, t); return CommandResult.Ok(); });
        Register("dcc", DccAsync);
        Register("log", LogAsync);
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

        await c.Session.SendRawAsync("JOIN " + NetworkProfile.NormalizeJoinSpec(c.Arguments), t).ConfigureAwait(false);
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
        await c.Session.WhoisAsync(nick, t).ConfigureAwait(false);
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
            c.Session.Print(c.Buffer, ChatLineKind.Info,
                "Current theme: " + c.Session.Theme.Id + " — /theme <id>  or View → Theme → Edit theme…");
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

    private static Task<CommandResult> AutoJoinAsync(CommandContext c, CancellationToken _)
    {
        var network = c.Session.Network;
        var args = c.Args;
        if (args.Length == 0 || args[0] is "toggle")
        {
            if (c.Buffer.Kind != BufferKind.Channel)
            {
                return Task.FromResult(CommandResult.Fail("Usage: /autojoin [add|del|list] [#channel]"));
            }

            return ApplyAutoJoin(c, c.Buffer.Name, !network.HasAutoJoin(c.Buffer.Name));
        }

        var verb = args[0];
        if (verb is "list" or "show")
        {
            var list = network.AutoJoin.Count == 0 ? "(none)" : string.Join(", ", network.AutoJoin);
            return Task.FromResult(CommandResult.Ok("Auto-join on " + network.Name + ": " + list));
        }

        var named = args.Length > 1 ? args[1] : (LooksLikeChannel(verb) ? verb : null);
        if (verb is "add" or "+")
        {
            named ??= c.Buffer.Kind == BufferKind.Channel ? c.Buffer.Name : null;
            return string.IsNullOrEmpty(named)
                ? Task.FromResult(CommandResult.Fail("Usage: /autojoin add [#channel]"))
                : ApplyAutoJoin(c, named, true);
        }

        if (verb is "del" or "remove" or "rm" or "-")
        {
            named ??= c.Buffer.Kind == BufferKind.Channel ? c.Buffer.Name : null;
            return string.IsNullOrEmpty(named)
                ? Task.FromResult(CommandResult.Fail("Usage: /autojoin del [#channel]"))
                : ApplyAutoJoin(c, named, false);
        }

        if (LooksLikeChannel(verb))
        {
            return ApplyAutoJoin(c, verb, true);
        }

        return Task.FromResult(CommandResult.Fail("Usage: /autojoin [add|del|list] [#channel]"));
    }

    private static Task<CommandResult> ApplyAutoJoin(CommandContext c, string channel, bool enable)
    {
        c.Session.Network.SetAutoJoin(channel, enable);
        c.Session.Persist();
        var name = NetworkProfile.AutoJoinChannelName(channel);
        return Task.FromResult(CommandResult.Ok(
            enable
                ? "Added " + name + " to auto-join for " + c.Session.Network.Name
                : "Removed " + name + " from auto-join for " + c.Session.Network.Name));
    }

    private static bool LooksLikeChannel(string value) =>
        value.Length > 0 && value[0] is '#' or '&' or '+' or '!';

    private static async Task<CommandResult> DccAsync(CommandContext c, CancellationToken t)
    {
        if (c.Session.Dcc is null)
        {
            return CommandResult.Fail("File transfer is not available.");
        }

        if (c.Args.Length == 0 || c.Args[0] is "list" or "transfers")
        {
            var n = c.Session.Dcc.Transfers.Count;
            return CommandResult.Ok(n == 0 ? "No file transfers." : n + " transfer(s). Open File → File transfers.");
        }

        var verb = c.Args[0];
        if (verb is "send")
        {
            if (c.Args.Length < 3)
            {
                return CommandResult.Fail("Usage: /dcc send <nick> <file path>");
            }

            var nick = c.Args[1];
            var path = c.Arguments[(c.Args[0].Length + 1 + nick.Length)..].Trim();
            if (path.StartsWith('"') && path.EndsWith('"') && path.Length > 1)
            {
                path = path[1..^1];
            }

            try
            {
                await c.Session.Dcc.SendFileAsync(c.Session, nick, path, t).ConfigureAwait(false);
                return CommandResult.Ok("Offered " + Path.GetFileName(path) + " to " + nick + ".");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail(ex.Message);
            }
        }

        if (verb is "close" or "cancel")
        {
            var open = c.Session.Dcc.Transfers.FirstOrDefault(x => !x.IsFinished);
            if (open is null)
            {
                return CommandResult.Fail("No active transfer.");
            }

            c.Session.Dcc.Cancel(open);
            return CommandResult.Ok("Cancelled " + open.FileName + ".");
        }

        return CommandResult.Fail("Usage: /dcc send <nick> <file>  |  /dcc list  |  /dcc cancel");
    }

    private static Task<CommandResult> LogAsync(CommandContext c, CancellationToken _)
    {
        if (c.Session.Logs is null)
        {
            return Task.FromResult(CommandResult.Fail("Logging is not available."));
        }

        var path = c.Session.Logs.PathFor(c.Session, c.Buffer);
        if (c.Args.Length > 0 && c.Args[0] is "off" or "stop")
        {
            c.Session.Settings.LogBuffers = false;
            c.Session.Persist();
            return Task.FromResult(CommandResult.Ok("Stopped logging. Folder: " + c.Session.Logs.Root));
        }

        if (c.Args.Length > 0 && c.Args[0] is "on" or "start")
        {
            c.Session.Settings.LogBuffers = true;
            c.Session.Persist();
            return Task.FromResult(CommandResult.Ok("Logging on. This window: " + path));
        }

        var state = c.Session.Settings.LogBuffers ? "on" : "off";
        return Task.FromResult(CommandResult.Ok("Logging is " + state + ". This window: " + path));
    }
}
