using PureFusionIRC.Core.Buffers;

namespace PureFusionIRC.Core.Irc;

public sealed partial class IrcSession
{
    private async Task HandleAsync(IrcMessage message, CancellationToken cancellationToken)
    {
        switch (message.Command.ToUpperInvariant())
        {
            case "PING":
                await SendRawAsync("PONG :" + (message.Trailing ?? string.Empty), cancellationToken)
                    .ConfigureAwait(false);
                return;
            case "PONG":
                HandlePong(message);
                return;
            case "CAP":
                await HandleCapAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            case "AUTHENTICATE":
                await HandleAuthenticateAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            case "PRIVMSG":
                HandlePrivmsg(message);
                return;
            case "NOTICE":
                HandleNotice(message);
                return;
            case "JOIN":
                HandleJoin(message);
                return;
            case "PART":
                HandlePart(message);
                return;
            case "QUIT":
                HandleQuit(message);
                return;
            case "NICK":
                HandleNick(message);
                return;
            case "MODE":
                HandleMode(message);
                return;
            case "TOPIC":
                HandleTopic(message);
                return;
            case "KICK":
                HandleKick(message);
                return;
            case "INVITE":
                Print(ServerBuffer, ChatLineKind.Info,
                    $"{message.Prefix?.Nick} invites you to {message.Trailing ?? message[1]}");
                return;
            case "ERROR":
                Print(ServerBuffer, ChatLineKind.Error, message.Trailing ?? "ERROR");
                return;
            case IrcNumerics.Welcome:
                CurrentNick = message[0] ?? CurrentNick;
                SetState(SessionState.Connected);
                Print(ServerBuffer, ChatLineKind.Server, message.Trailing ?? string.Empty);
                await AfterWelcomeAsync(cancellationToken).ConfigureAwait(false);
                return;
            case IrcNumerics.YourHost:
            case IrcNumerics.Created:
            case IrcNumerics.MyInfo:
            case IrcNumerics.LUserClient:
            case IrcNumerics.LUserOp:
            case IrcNumerics.LUserUnknown:
            case IrcNumerics.LUserChannels:
            case IrcNumerics.LUserMe:
            case IrcNumerics.LocalUsers:
            case IrcNumerics.GlobalUsers:
                Print(ServerBuffer, ChatLineKind.Server, string.Join(' ', message.Parameters.Skip(1)));
                return;
            case IrcNumerics.ISupport:
                HandleISupport(message);
                return;
            case IrcNumerics.Topic:
                HandleTopicNumeric(message);
                return;
            case IrcNumerics.TopicWhoTime:
                return;
            case IrcNumerics.NameReply:
                HandleNames(message);
                return;
            case IrcNumerics.EndOfNames:
                await HandleEndOfNamesAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            case IrcNumerics.WhoReply:
                HandleWho(message);
                return;
            case IrcNumerics.WhoXReply:
                HandleWhoX(message);
                return;
            case IrcNumerics.EndOfWho:
                return;
            case "AWAY":
                HandleAwayNotify(message);
                return;
            case IrcNumerics.MotdStart:
            case IrcNumerics.Motd:
            case IrcNumerics.EndOfMotd:
            case IrcNumerics.NoMotd:
                if (Settings.ShowMotd)
                {
                    Print(ServerBuffer, ChatLineKind.Motd, message.Trailing ?? string.Empty);
                }

                return;
            case IrcNumerics.NicknameInUse:
            case IrcNumerics.ErroneousNickname:
                await HandleNickInUseAsync(cancellationToken).ConfigureAwait(false);
                return;
            case IrcNumerics.UModeIs:
                UserModes = message[1];
                return;
            case IrcNumerics.WhoisIdle:
                HandleWhoisIdle(message);
                Print(ServerBuffer, ChatLineKind.Info, string.Join(' ', message.Parameters.Skip(1)));
                return;
            case IrcNumerics.WhoisUser:
            case IrcNumerics.WhoisServer:
            case IrcNumerics.WhoisOperator:
            case IrcNumerics.WhoisChannels:
            case IrcNumerics.WhoisAccount:
            case IrcNumerics.EndOfWhois:
                Print(ServerBuffer, ChatLineKind.Info, string.Join(' ', message.Parameters.Skip(1)));
                return;
            case IrcNumerics.Away:
                HandleWhoisAway(message);
                Print(ServerBuffer, ChatLineKind.Info, string.Join(' ', message.Parameters.Skip(1)));
                return;
            case IrcNumerics.SaslSuccess:
            case IrcNumerics.LoggedIn:
                _saslDone = true;
                Print(ServerBuffer, ChatLineKind.Info, message.Trailing ?? "SASL ok");
                if (!_capEnded)
                {
                    await SendRawAsync("CAP END", cancellationToken).ConfigureAwait(false);
                    _capEnded = true;
                }

                return;
            case IrcNumerics.SaslFail:
            case IrcNumerics.SaslAborted:
            case IrcNumerics.SaslTooLong:
            case IrcNumerics.SaslAlready:
                Print(ServerBuffer, ChatLineKind.Error, message.Trailing ?? "SASL failed");
                if (!_capEnded)
                {
                    await SendRawAsync("CAP END", cancellationToken).ConfigureAwait(false);
                    _capEnded = true;
                }

                return;
            default:
                if (message.Command.Length == 3 && char.IsDigit(message.Command[0]))
                {
                    var kind = message.Command[0] == '4' || message.Command[0] == '5'
                        ? ChatLineKind.Error
                        : ChatLineKind.Server;
                    Print(ServerBuffer, kind, string.Join(' ', message.Parameters.Skip(1)));
                }
                else
                {
                    Print(ServerBuffer, ChatLineKind.Server, message.FormatOutgoing());
                }

                return;
        }
    }

    private async Task HandleCapAsync(IrcMessage message, CancellationToken cancellationToken)
    {
        var sub = message.Parameters.Count >= 2 ? message.Parameters[1] : message.Trailing;
        if (string.Equals(sub, "LS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sub, "ACK", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sub, "NAK", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sub, "NEW", StringComparison.OrdinalIgnoreCase))
        {
            var caps = (message.Trailing ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (string.Equals(sub, "LS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sub, "NEW", StringComparison.OrdinalIgnoreCase))
            {
                var want = new List<string>();
                foreach (var cap in caps)
                {
                    var name = cap.Split('=', 2)[0];
                    if (name is "multi-prefix" or "server-time" or "account-tag" or "extended-join"
                        or "away-notify" or "chghost" or "message-tags" or "userhost-in-names")
                    {
                        want.Add(name);
                    }

                    if (name == "sasl" && !string.IsNullOrEmpty(Network.SaslAccount))
                    {
                        want.Add("sasl");
                    }
                }

                RequestedCapabilities = want;
                if (want.Count > 0)
                {
                    await SendRawAsync("CAP REQ :" + string.Join(' ', want), cancellationToken).ConfigureAwait(false);
                }
                else if (!_capEnded)
                {
                    await SendRawAsync("CAP END", cancellationToken).ConfigureAwait(false);
                    _capEnded = true;
                }
            }
            else if (string.Equals(sub, "ACK", StringComparison.OrdinalIgnoreCase))
            {
                _pendingCaps.Clear();
                _pendingCaps.AddRange(caps);
                if (_pendingCaps.Contains("sasl", StringComparer.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(Network.SaslAccount))
                {
                    await SendRawAsync("AUTHENTICATE PLAIN", cancellationToken).ConfigureAwait(false);
                }
                else if (!_capEnded)
                {
                    await SendRawAsync("CAP END", cancellationToken).ConfigureAwait(false);
                    _capEnded = true;
                }
            }
            else if (string.Equals(sub, "NAK", StringComparison.OrdinalIgnoreCase) && !_capEnded)
            {
                await SendRawAsync("CAP END", cancellationToken).ConfigureAwait(false);
                _capEnded = true;
            }
        }
    }

    private async Task HandleAuthenticateAsync(IrcMessage message, CancellationToken cancellationToken)
    {
        if (_saslDone)
        {
            return;
        }

        if (message.Trailing == "+" || message[0] == "+")
        {
            var account = Network.SaslAccount ?? CurrentNick;
            var password = Network.SaslPassword ?? string.Empty;
            await SendRawAsync("AUTHENTICATE " + EncodeSaslPlain(account, password), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private void HandlePong(IrcMessage message)
    {
        var token = message.Trailing ?? string.Empty;
        if (token.StartsWith("pf", StringComparison.Ordinal) &&
            long.TryParse(token[2..], out var sent))
        {
            Lag = TimeSpan.FromMilliseconds(Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sent));
            Print(ServerBuffer, ChatLineKind.Info, $"Lag: {Lag.TotalMilliseconds:0} ms");
        }
    }

    private void HandlePrivmsg(IrcMessage message)
    {
        var from = message.Prefix?.Nick ?? message.Prefix?.Raw ?? "?";
        var target = message[0] ?? string.Empty;
        var text = message.Trailing ?? string.Empty;
        var ctcp = CtcpPayload(text);
        if (ctcp is not null)
        {
            HandleCtcp(from, target, ctcp, reply: false);
            return;
        }

        var toUs = string.Equals(target, CurrentNick, StringComparison.OrdinalIgnoreCase);
        var buffer = toUs
            ? GetOrCreate(BufferKind.Query, from)
            : GetOrCreate(BufferKind.Channel, target);
        Print(buffer, ChatLineKind.Message, text, from);
    }

    private void HandleNotice(IrcMessage message)
    {
        var from = message.Prefix?.Nick ?? message.Prefix?.Raw ?? "notice";
        var target = message[0] ?? string.Empty;
        var text = message.Trailing ?? string.Empty;
        var ctcp = CtcpPayload(text);
        if (ctcp is not null)
        {
            HandleCtcp(from, target, ctcp, reply: true);
            return;
        }

        var buffer = string.Equals(target, CurrentNick, StringComparison.OrdinalIgnoreCase) && message.Prefix?.IsUser == true
            ? GetOrCreate(BufferKind.Query, from)
            : ServerBuffer;
        Print(buffer, ChatLineKind.Notice, text, from);
    }

    private void HandleCtcp(string from, string target, string payload, bool reply)
    {
        var space = payload.IndexOf(' ');
        var command = space < 0 ? payload : payload[..space];
        var args = space < 0 ? string.Empty : payload[(space + 1)..];
        if (reply)
        {
            Print(ServerBuffer, ChatLineKind.Ctcp, $"CTCP {command} reply from {from}: {args}");
            return;
        }

        Print(ServerBuffer, ChatLineKind.Ctcp, $"CTCP {command} from {from}");
        var response = command.ToUpperInvariant() switch
        {
            "VERSION" => "VERSION PureFusionIRC 0.1.0",
            "TIME" => "TIME " + DateTime.Now.ToString("R"),
            "PING" => "PING " + args,
            "CLIENTINFO" => "CLIENTINFO VERSION TIME PING CLIENTINFO",
            _ => null
        };

        if (response is not null)
        {
            _ = SendRawAsync($"NOTICE {from} :\u0001{response}\u0001");
        }

        if (command.Equals("ACTION", StringComparison.OrdinalIgnoreCase))
        {
            var toUs = string.Equals(target, CurrentNick, StringComparison.OrdinalIgnoreCase);
            var buffer = toUs ? GetOrCreate(BufferKind.Query, from) : GetOrCreate(BufferKind.Channel, target);
            Print(buffer, ChatLineKind.Action, args, from);
        }
    }

    private void HandleJoin(IrcMessage message)
    {
        var nick = message.Prefix?.Nick ?? "?";
        var channel = message.Trailing ?? message[0] ?? string.Empty;
        var buffer = GetOrCreate(BufferKind.Channel, channel);
        if (!Settings.HideJoinPart)
        {
            Print(buffer, ChatLineKind.Join, nick + " has joined " + channel, nick);
        }

        if (string.Equals(nick, CurrentNick, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        buffer.UpsertNick(new NickEntry(nick));
    }

    private void HandlePart(IrcMessage message)
    {
        var nick = message.Prefix?.Nick ?? "?";
        var channel = message[0] ?? string.Empty;
        var buffer = GetOrCreate(BufferKind.Channel, channel);
        if (!Settings.HideJoinPart)
        {
            Print(buffer, ChatLineKind.Part, nick + " has left " + channel + (string.IsNullOrEmpty(message.Trailing) ? string.Empty : " (" + message.Trailing + ")"), nick);
        }

        if (string.Equals(nick, CurrentNick, StringComparison.OrdinalIgnoreCase))
        {
            CloseBuffer(buffer);
            return;
        }

        buffer.RemoveNick(nick);
    }

    private void HandleQuit(IrcMessage message)
    {
        var nick = message.Prefix?.Nick ?? "?";
        var reason = message.Trailing ?? string.Empty;
        foreach (var buffer in Buffers.Where(b => b.Kind == BufferKind.Channel && b.NickMap.ContainsKey(nick)).ToList())
        {
            if (!Settings.HideJoinPart)
            {
                Print(buffer, ChatLineKind.Quit, nick + " has quit (" + reason + ")", nick);
            }

            buffer.RemoveNick(nick);
        }
    }

    private void HandleNick(IrcMessage message)
    {
        var oldNick = message.Prefix?.Nick ?? "?";
        var newNick = message.Trailing ?? message[0] ?? string.Empty;
        if (string.Equals(oldNick, CurrentNick, StringComparison.OrdinalIgnoreCase))
        {
            CurrentNick = newNick;
        }

        foreach (var buffer in Buffers.Where(b => b.Kind == BufferKind.Channel && b.NickMap.ContainsKey(oldNick)).ToList())
        {
            Print(buffer, ChatLineKind.Nick, oldNick + " is now known as " + newNick, oldNick);
            buffer.RenameNick(oldNick, newNick);
        }
    }

    private void HandleMode(IrcMessage message)
    {
        var target = message[0] ?? string.Empty;
        var modes = string.Join(' ', message.Parameters.Skip(1));
        if (target.StartsWith('#') || target.StartsWith('&'))
        {
            var buffer = GetOrCreate(BufferKind.Channel, target);
            Print(buffer, ChatLineKind.Mode, (message.Prefix?.Nick ?? "server") + " sets mode " + modes);
            ApplyChannelModes(buffer, message);
        }
        else
        {
            UserModes = modes;
            Print(ServerBuffer, ChatLineKind.Mode, "User mode: " + modes);
        }
    }

    private void ApplyChannelModes(IrcBuffer buffer, IrcMessage message)
    {
        if (message.Parameters.Count < 2)
        {
            return;
        }

        var spec = message.Parameters[1];
        var argIndex = 2;
        var adding = true;
        foreach (var ch in spec)
        {
            if (ch == '+')
            {
                adding = true;
                continue;
            }

            if (ch == '-')
            {
                adding = false;
                continue;
            }

            var prefix = PrefixForMode(ch);
            if (prefix == '\0' || argIndex >= message.Parameters.Count)
            {
                if ("beIklovqh".Contains(ch) && argIndex < message.Parameters.Count)
                {
                    argIndex++;
                }

                continue;
            }

            var nick = message.Parameters[argIndex++];
            if (adding)
            {
                buffer.AddPrefix(nick, prefix);
            }
            else
            {
                buffer.RemovePrefix(nick, prefix);
            }
        }
    }

    private void HandleTopic(IrcMessage message)
    {
        var channel = message[0] ?? string.Empty;
        var buffer = GetOrCreate(BufferKind.Channel, channel);
        buffer.Topic = message.Trailing;
        Print(buffer, ChatLineKind.Topic, (message.Prefix?.Nick ?? "server") + " set topic: " + message.Trailing);
    }

    private void HandleTopicNumeric(IrcMessage message)
    {
        var channel = message[1] ?? string.Empty;
        var buffer = GetOrCreate(BufferKind.Channel, channel);
        buffer.Topic = message.Trailing;
        Print(buffer, ChatLineKind.Topic, "Topic: " + message.Trailing);
    }

    private void HandleKick(IrcMessage message)
    {
        var channel = message[0] ?? string.Empty;
        var kicked = message[1] ?? string.Empty;
        var buffer = GetOrCreate(BufferKind.Channel, channel);
        Print(buffer, ChatLineKind.Kick, $"{message.Prefix?.Nick} kicked {kicked} ({message.Trailing})");
        if (string.Equals(kicked, CurrentNick, StringComparison.OrdinalIgnoreCase))
        {
            CloseBuffer(buffer);
        }
        else
        {
            buffer.RemoveNick(kicked);
        }
    }

    private void HandleNames(IrcMessage message)
    {
        var channel = message[2] ?? string.Empty;
        var buffer = GetOrCreate(BufferKind.Channel, channel);
        var nicks = (message.Trailing ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in nicks)
        {
            var i = 0;
            while (i < token.Length && PrefixSymbols.Contains(token[i]))
            {
                i++;
            }

            var prefixes = token[..i];
            var nick = token[i..];
            if (nick.Length > 0)
            {
                buffer.UpsertNick(new NickEntry(nick, prefixes));
            }
        }
    }

    private Task HandleEndOfNamesAsync(IrcMessage message, CancellationToken cancellationToken)
    {
        var channel = message[1] ?? string.Empty;
        if (string.IsNullOrEmpty(channel) || channel.StartsWith(':'))
        {
            return Task.CompletedTask;
        }

        return SendRawAsync("WHO " + channel, cancellationToken);
    }

    private void HandleWho(IrcMessage message)
    {
        var channel = message[1] ?? string.Empty;
        var nick = message[5] ?? string.Empty;
        var flags = message[6] ?? string.Empty;
        if (string.IsNullOrEmpty(nick) || channel.Length == 0 || channel[0] is not ('#' or '&' or '+'))
        {
            return;
        }

        var buffer = GetOrCreate(BufferKind.Channel, channel);
        var away = flags.Contains('G');
        var prefixes = NickEntry.NormalizePrefixes(flags);
        if (!buffer.NickMap.ContainsKey(nick))
        {
            buffer.UpsertNick(new NickEntry(nick, prefixes) { Away = away });
            return;
        }

        buffer.ApplyPresence(nick, away, prefixes: prefixes.Length > 0 ? prefixes : null);
    }

    private void HandleWhoX(IrcMessage message)
    {
        // Idle fields filled in the WHOX follow-up.
    }

    private void HandleAwayNotify(IrcMessage message)
    {
        var nick = message.Prefix?.Nick;
        if (string.IsNullOrEmpty(nick))
        {
            return;
        }

        var away = !string.IsNullOrEmpty(message.Trailing);
        foreach (var buffer in Buffers.Where(b => b.Kind == BufferKind.Channel))
        {
            buffer.ApplyPresence(nick, away);
        }
    }

    private void HandleWhoisAway(IrcMessage message)
    {
        var nick = message[1];
        if (string.IsNullOrEmpty(nick))
        {
            return;
        }

        foreach (var buffer in Buffers.Where(b => b.Kind == BufferKind.Channel))
        {
            buffer.ApplyPresence(nick, away: true);
        }
    }

    private void HandleWhoisIdle(IrcMessage message)
    {
        // Idle seconds applied in the idle follow-up.
    }

    private void HandleISupport(IrcMessage message)
    {
        foreach (var token in message.Parameters.Skip(1))
        {
            if (token.StartsWith(':'))
            {
                break;
            }

            var parts = token.Split('=', 2);
            ISupport[parts[0]] = parts.Length > 1 ? parts[1] : string.Empty;
            if (parts[0] == "PREFIX" && parts.Length > 1 && parts[1].StartsWith('('))
            {
                var end = parts[1].IndexOf(')');
                if (end > 1)
                {
                    PrefixLetters = parts[1][1..end];
                    PrefixSymbols = parts[1][(end + 1)..];
                }
            }
        }

        Print(ServerBuffer, ChatLineKind.Server, string.Join(' ', message.Parameters.Skip(1).TakeWhile(p => !p.StartsWith(':'))));
    }

    private async Task AfterWelcomeAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(Network.NickServPassword) && string.IsNullOrEmpty(Network.SaslAccount))
        {
            await SendRawAsync("PRIVMSG NickServ :IDENTIFY " + Network.NickServPassword, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var channel in Network.AutoJoin)
        {
            if (!string.IsNullOrWhiteSpace(channel))
            {
                await SendRawAsync("JOIN " + channel, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleNickInUseAsync(CancellationToken cancellationToken)
    {
        _nickTries++;
        var next = _nickTries == 1 && !string.IsNullOrEmpty(Identity.AlternativeNick)
            ? Identity.AlternativeNick
            : CurrentNick + "_";
        CurrentNick = next;
        Print(ServerBuffer, ChatLineKind.Error, "Nickname in use, trying " + next);
        await SendRawAsync("NICK " + next, cancellationToken).ConfigureAwait(false);
    }
}
