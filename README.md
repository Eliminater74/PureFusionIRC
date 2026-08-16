# PureFusionIRC

A Windows-only IRC client written in C# / WPF. The layout, feel, and daily workflow are modeled on **mIRC**, with a full theme engine, modern protocol support, and room to grow scripts and plugins — without copying HexChat or mIRC source.

HexChat source in `TEMP/` is **reference only** and is gitignored. mIRC has no public source; the UI follows its visual language: tree of servers/channels, chat pane, nick list, input box, menus, and status bar.

## Status

Core client: connect over TCP or TLS, register, talk, join channels, nick list, commands, themes, settings import/export, and a JavaScript script host. See [ROADMAP.md](ROADMAP.md) and [TODO.md](TODO.md) for what comes next (DCC, ident, fuller IRCv3, plugin DLLs).

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later (SDK 8, 9, and 10 are fine)
- Visual Studio 2022, Cursor, or `dotnet` CLI

## Build and run

```powershell
dotnet restore PureFusionIRC.slnx
dotnet build PureFusionIRC.slnx -c Release
dotnet run --project src/PureFusionIRC.App -c Release
```

Data lives under `%AppData%\PureFusionIRC\` (settings, networks, themes, scripts, logs). Built-in themes are shipped with the app; user themes can be added without rebuilding.

## What it is trying to be

| Area | Approach |
| --- | --- |
| Look and feel | mIRC: classic menus, tree + chat + nicklist + input, not a GTK port |
| Protocol | RFC 1459 / 2812 style messaging, TLS, CAP, SASL PLAIN, CTCP, ISUPPORT |
| Themes | JSON theme engine. Default is **AMOLED Black** (true black + white). Also **Classic Light** and **Charcoal** |
| Scripts | JavaScript (`.pf.js`) via an embedded engine — not mIRC script, not HexChat Python/Perl |
| Plugins | Folder + interface stub now; loadable assemblies later |
| Settings | JSON + DPAPI-protected secrets on Windows; export/import a zip pack |

## Default themes

- **AMOLED Black** — `#000000` chrome and chat, white text (default)
- **Classic Light** — mIRC-like pale panels, dark text
- **Charcoal** — softer dark gray, not OLED-black

Switch from **View → Theme** or `/theme <id>`. Export/import themes with the rest of your settings from **Tools → Export settings** / **Import settings**.

## Layout

```
[ File  View  Tools  Window  Help ]
[ toolbar: connect / disconnect / networks / options ]
+-----------+---------------------------+-----------+
| tree      | chat (mIRC color codes)   | nick list |
|  server   |                           | @ops      |
|   #chan   |                           | +voice    |
|   query   |                           | users     |
+-----------+---------------------------+-----------+
| input  (/commands or message to current buffer)   |
| status: nick  modes  lag  users  server           |
```

## Commands (subset)

`/server`, `/disconnect`, `/join`, `/part`, `/quit`, `/nick`, `/me`, `/msg`, `/query`, `/notice`, `/ctcp`, `/whois`, `/mode`, `/topic`, `/kick`, `/invite`, `/quote`, `/clear`, `/theme`, `/help`

Bare text is sent to the current channel or query. `//text` sends a line that starts with `/`.

## Scripts

Put `.pf.js` files in `%AppData%\PureFusionIRC\scripts\`. They get `irc.on`, `irc.command`, and `irc.print`. Example:

```javascript
irc.on("message", function (e) {
  if (e.text && e.text.indexOf("hello bot") >= 0) {
    irc.command("/msg " + e.target + " Hello from PureFusionIRC");
  }
});
```

## Project layout

```
src/PureFusionIRC.Core   protocol, buffers, settings, themes, scripts
src/PureFusionIRC.App    WPF shell (Windows only)
tests/PureFusionIRC.Core.Tests
themes/                  default theme JSON (copied into the app)
```

## License

MIT. See [LICENSE](LICENSE). HexChat (in `TEMP/`, not shipped) remains under its own GPL terms and is not incorporated into this tree.
