# PureFusionIRC

[![Release](https://img.shields.io/github/v/release/Eliminater74/PureFusionIRC?include_prereleases&label=release)](https://github.com/Eliminater74/PureFusionIRC/releases/latest)
[![Installer and zip downloads](https://img.shields.io/github/downloads/Eliminater74/PureFusionIRC/total?logo=github&label=downloads)](https://github.com/Eliminater74/PureFusionIRC/releases)
[![Latest release downloads](https://img.shields.io/github/downloads/Eliminater74/PureFusionIRC/latest/total?label=latest%20release)](https://github.com/Eliminater74/PureFusionIRC/releases/latest)
[![Visitors](https://api.visitorbadge.io/api/visitors?path=Eliminater74%2FPureFusionIRC&label=visitors&countColor=%231E88E5)](https://github.com/Eliminater74/PureFusionIRC)
[![CI](https://img.shields.io/github/actions/workflow/status/Eliminater74/PureFusionIRC/ci.yml?branch=main&label=CI)](https://github.com/Eliminater74/PureFusionIRC/actions)
[![Stars](https://img.shields.io/github/stars/Eliminater74/PureFusionIRC)](https://github.com/Eliminater74/PureFusionIRC/stargazers)
[![Issues](https://img.shields.io/github/issues/Eliminater74/PureFusionIRC)](https://github.com/Eliminater74/PureFusionIRC/issues)
[![License](https://img.shields.io/github/license/Eliminater74/PureFusionIRC)](LICENSE)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows&logoColor=white)](#requirements)
[![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)](#requirements)

A Windows-only IRC client written in C# / WPF. The layout, feel, and daily workflow are modeled on **mIRC**, with a JSON theme engine you can edit in the app, modern IRCv3 support, and room to grow scripts and plugins — without copying HexChat or mIRC source.

HexChat source in `TEMP/` is **reference only** and is gitignored. mIRC has no public source; the UI follows its visual language: tree of servers/channels, chat pane, nick list, input box, menus, and status bar.

## Status

**v1.0.0-B3 (beta 3)** is the current [GitHub Release](https://github.com/Eliminater74/PureFusionIRC/releases/tag/v1.0.0-B3).

## Stats

GitHub counts every fetch of a release asset (the Inno `*-setup.exe` and the portable zip). **Help → Check for updates** in the client downloads that same setup.exe, so those installs show up here too.

- **downloads** — all assets on all releases
- **latest release** — assets on whatever GitHub marks Latest
- **visitors** — README / repo page hits (approximate; GitHub caches badge images)
- In the client: **Help → About PureFusionIRC** splits installer vs zip and also shows stars, forks, watchers, and open issues

Repo traffic (clones and unique visitors with more detail) is under GitHub **Insights → Traffic** if you are a collaborator.

Shipped: TCP/TLS connect, identd, SASL PLAIN, NickServ identify, IRCv3 (replies, react, echo-message, chat history, batches, labeled WHOIS), channels and queries, nick list, mIRC colors, clickable URLs, commands, in-app theme editor, settings export/import, JavaScript scripts, tray, reverse-first DCC SEND, daily logs, Inno installer, and in-app updates.

Not in this build: DCC CHAT, DCC RESUME, loadable plugin DLLs, `/list` UI, banlist windows, switchbar tabs, and search-in-buffer. See [ROADMAP.md](ROADMAP.md) and [TODO.md](TODO.md).

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later (SDK 8, 9, and 10 are fine)
- Visual Studio 2022, Cursor, or `dotnet` CLI

## Build and run (Windows `.exe`)

This is a **Windows WinExe**, like mIRC. The build product is `PureFusionIRC.exe`, not a macOS `.app`.

```powershell
dotnet restore PureFusionIRC.slnx
dotnet build PureFusionIRC.slnx -c Release
dotnet run --project src/PureFusionIRC.App -c Release
```

The executable is:

`src\PureFusionIRC.App\bin\Release\net8.0-windows\PureFusionIRC.exe`

To publish a runnable folder (still `.exe`):

```powershell
dotnet publish src/PureFusionIRC.App -c Release -r win-x64 --self-contained false
```

`src/PureFusionIRC.App` is only the C# WPF project name (`App.xaml`). It is not an Apple app bundle.

All user data lives under `%AppData%\PureFusionIRC\`:

| Folder | What |
| --- | --- |
| `settings.json` / `networks.json` | Options and server list (secrets DPAPI-protected) |
| `themes\` | Built-in copies + your edited JSON |
| `scripts\` | JavaScript (`.pf.js`) |
| `logs\` | Daily chat logs |
| `transfers\` | Default DCC save folder |
| `plugins\` | Stub folder for future DLLs |

## Installer (Inno Setup)

GitHub Releases ship `PureFusionIRC-<version>-setup.exe` (self-contained x64) and a portable zip. Current beta is **v1.0.0-B3**.

To build the setup locally, install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then:

```powershell
powershell -ExecutionPolicy Bypass -File packaging/build-installer.ps1
```

Output:

- `artifacts\installer\PureFusionIRC-1.0.0-B3-setup.exe`
- `artifacts\portable\PureFusionIRC-1.0.0-B3-win-x64.zip`

User settings stay in `%AppData%\PureFusionIRC\` and are not removed on uninstall.

## GitHub Actions releases

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs tests on Windows, compiles the Inno installer, and publishes a GitHub Release from a `v*` tag (latest Release, not hidden as prerelease).

```powershell
git tag v1.0.0-B3
git push origin main --tags
```

You can also run **Actions → Build and release → Run workflow**, keep version `1.0.0-B3`, and enable **Create a GitHub Release**.

## What it is trying to be

| Area | Approach |
| --- | --- |
| Look and feel | mIRC: classic menus, tree + chat + nicklist + input, not a GTK port |
| Protocol | RFC 1459 / 2812, TLS, identd, CAP, SASL PLAIN, CTCP, ISUPPORT, IRCv3 tags |
| Themes | JSON + in-app editor. Default **AMOLED Black**. Also **Classic Light** and **Charcoal** |
| Scripts | JavaScript (`.pf.js`) via an embedded engine — not mIRC script, not HexChat Python/Perl |
| Plugins | Folder stub now; loadable assemblies later |
| Settings | JSON + DPAPI-protected secrets; export/import a zip pack |

## Layout

```
[ File  View  Tools  Channel  Help ]
[ toolbar: Networks / Connect / Disconnect / Options / Transfers / Auto-join ]
+-----------+---------------------------+-----------+
| tree      | chat (mIRC color codes)   | nick list |
|  server   | timestamps, links         | @ops      |
|   #chan   | right-click Reply / React | +voice    |
|   query   |                           | users     |
+-----------+---------------------------+-----------+
| input  (/commands, @nick picker, Tab complete)    |
| status: nick  modes  lag  users  server           |
```

**View** hides the tree, nick list, or toolbar. **File → Networks…** is the server list (countries, TLS, SASL, NickServ, auto-join, connect on startup). **Help → What's new…** is this changelog; **Check for updates…** downloads a newer setup.exe; **About** shows live GitHub download and star counts.

## Themes

- **AMOLED Black** — true-black chat, navy chrome, cyan button/menu accents (default)
- **Classic Light** — pale blue chrome, dark text
- **Charcoal** — softer dark gray panels with bluish controls

Switch from **View → Theme**, `/theme <id>`, or edit every color from:

- **View → Theme → Edit theme…**
- **Tools → Theme editor…**
- **Tools → Options → Edit colors…**

The main window updates live. **Save** writes `%AppData%\PureFusionIRC\themes\`. Duplicate a stock theme before heavy edits; **Reset to factory** only applies to built-in ids. Startup no longer overwrites those JSON files. **Fonts** (family and size) stay in **Tools → Options**.

## Connect, identd, and IRCv3

- **Identd** listens on TCP 113 before login (Options → Identity). IRCnet often waits on this. Windows usually needs Administrator to bind 113; a bind failure is printed and connect still continues.
- **SASL PLAIN** and **NickServ IDENTIFY** are per-network in the Networks window.
- Capabilities requested when the server offers them include: `multi-prefix`, `server-time`, `account-tag`, `extended-join`, `away-notify`, `chghost`, `message-tags`, `userhost-in-names`, `echo-message`, `account-notify`, `invite-notify`, `cap-notify`, `labeled-response`, `batch`, `setname`, `chathistory` / `draft/chathistory`, `draft/multiline`, `standard-replies`, and `sasl` when you set an account.
- Right-click a chat line: **Reply** (sends `+draft/reply` when tags are on), **React 👍**, Query, Whois.
- `/join c-64` and auto-join accept a channel with or without `#`.

## Commands

Bare text goes to the current channel or query. `//text` sends a line that starts with `/`.

`/help`, `/join` (`/j`), `/part`, `/quit`, `/disconnect`, `/reconnect`, `/nick`, `/me`, `/msg`, `/query`, `/notice`, `/ctcp`, `/whois`, `/whowas`, `/who`, `/mode`, `/topic`, `/kick`, `/invite`, `/quote` (`/raw`), `/names`, `/list`, `/away`, `/back`, `/ping`, `/clear`, `/theme`, `/echo`, `/say`, `/hop`, `/autojoin`, `/umode`, `/motd`, `/lusers`, `/links`, `/time`, `/version`, `/admin`, `/info`, `/stats`, `/dcc`, `/log`

`/list` talks to the server (text in the status buffer). There is no list window yet. Use **File → Networks…** instead of `/server`.

## File transfers

Direct transfers (DCC SEND) go computer-to-computer. Reverse send is **on by default**, so the other side opens a port and you connect out — typical home NAT does not need port forwarding to send.

- Right-click a nick → **Send file…**, or **File → Send file…** in a query
- Incoming files get a Save / Decline prompt and show in **File → File transfers…**
- Progress, speed, and ETA live in that window; Open jumps to the saved file
- Folder and reverse preference: **Tools → Options → Files**
- `/dcc send <nick> <path>`, `/dcc list`, `/dcc cancel`

DCC CHAT and RESUME are not implemented. Incoming files default to `%AppData%\PureFusionIRC\transfers\`.

## Logs

Chat is logged by default to `%AppData%\PureFusionIRC\logs\<network>\<yyyy-MM-dd>\<channel>.log` (UTF-8, timestamps, mIRC codes stripped). Queries and the server window get their own files. Turn it off in **Tools → Options → General**, open the folder from **Tools → Open logs folder**, or type `/log` (`/log off` / `/log on`).

## Options (Tools → Options)

- **General** — timestamps, reconnect, hide join/part, strip colors, logs, MOTD, tray (minimize / close / balloons), GitHub update check, timestamp format, font family and size, highlight words, theme editor shortcut
- **Files** — enable DCC, prefer reverse, download folder
- **Identity** — nick, alt nick, username, real name, identd

Your own nick is mint-green in the nick list. Mentions (your nick or highlight words) only paint **messages / actions / notices**, not join or topic lines.

## Updates

**Help → What's new…** shows [CHANGELOG.md](CHANGELOG.md).

**Help → Check for updates…** reads [GitHub Releases](https://github.com/Eliminater74/PureFusionIRC/releases). If a newer `PureFusionIRC-*-setup.exe` is posted, you can download it and run a silent Inno setup. That closes this copy, installs over Program Files, and launches the new build. Settings stay in AppData.

Startup checks are on by default.

## Scripts

Put `.pf.js` files in `%AppData%\PureFusionIRC\scripts\`. They get `irc.on`, `irc.command`, and `irc.print`. **Tools → Reload scripts** / **Open scripts folder**. Example:

```javascript
irc.on("message", function (e) {
  if (e.text && e.text.indexOf("hello bot") >= 0) {
    irc.command("/msg " + e.target + " Hello from PureFusionIRC");
  }
});
```

## Project layout

```
src/PureFusionIRC.Core   protocol, buffers, settings, themes, scripts, identd, DCC, updates
src/PureFusionIRC.App    WPF shell (Windows only)
tests/PureFusionIRC.Core.Tests
themes/                  factory theme JSON
packaging/               Inno ISS + build-installer.ps1
```

## License

MIT. See [LICENSE](LICENSE). HexChat (in `TEMP/`, not shipped) remains under its own GPL terms and is not incorporated into this tree.
