# PureFusionIRC

A Windows-only IRC client written in C# / WPF. The layout, feel, and daily workflow are modeled on **mIRC**, with a full theme engine, modern protocol support, and room to grow scripts and plugins — without copying HexChat or mIRC source.

HexChat source in `TEMP/` is **reference only** and is gitignored. mIRC has no public source; the UI follows its visual language: tree of servers/channels, chat pane, nick list, input box, menus, and status bar.

## Status

**v1.0.0-B1 (beta 1).** Core client: connect over TCP or TLS, register, talk, join channels, nick list, commands, themes, settings import/export, JavaScript scripts, tray, reverse-first file transfers (DCC), and an Inno Setup installer. See [ROADMAP.md](ROADMAP.md) and [TODO.md](TODO.md) for what comes next (DCC chat, ident, fuller IRCv3, plugin DLLs).

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

Data lives under `%AppData%\PureFusionIRC\` (settings, networks, themes, scripts, logs). Built-in themes are shipped with the app; user themes can be added without rebuilding.

## Installer (Inno Setup)

GitHub Releases ship `PureFusionIRC-<version>-setup.exe` (self-contained x64) and a portable zip. The first public beta is **v1.0.0-B1**.

To build the setup locally, install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then:

```powershell
powershell -ExecutionPolicy Bypass -File packaging/build-installer.ps1
```

Output:

- `artifacts\installer\PureFusionIRC-1.0.0-B1-setup.exe`
- `artifacts\portable\PureFusionIRC-1.0.0-B1-win-x64.zip`

User settings stay in `%AppData%\PureFusionIRC\` and are not removed on uninstall.

## GitHub Actions releases

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs tests on Windows, compiles the Inno installer, and can publish a GitHub Release.

Push a version tag to cut a release (beta tags stay marked prerelease):

```powershell
git tag v1.0.0-B1
git push origin main --tags
```

You can also run **Actions → Build and release → Run workflow**, keep version `1.0.0-B1`, and enable **Create a GitHub Release**.

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

- **AMOLED Black** — true-black chat, navy chrome, cyan button/menu accents (default)
- **Classic Light** — pale blue chrome, dark text
- **Charcoal** — softer dark gray panels with bluish controls

Switch from **View → Theme** or `/theme <id>`. Export/import themes with the rest of your settings from **Tools → Export settings** / **Import settings**.

## Layout

```
[ File  View  Tools  Window  Help ]
[ toolbar: connect / disconnect / networks / options / transfers ]
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

`/server`, `/disconnect`, `/join`, `/part`, `/quit`, `/nick`, `/me`, `/msg`, `/query`, `/notice`, `/ctcp`, `/whois`, `/mode`, `/topic`, `/kick`, `/invite`, `/quote`, `/clear`, `/theme`, `/autojoin`, `/dcc`, `/help`

Bare text is sent to the current channel or query. `//text` sends a line that starts with `/`.

### File transfers

Direct transfers (DCC SEND) go computer-to-computer. Reverse send is **on by default**, so the other side opens a port and you connect out — typical home NAT does not need port forwarding to send.

- Right-click a nick → **Send file…**, or **File → Send file…** in a query
- Incoming files get a Save / Decline prompt (not a raw CTCP dump) and show in **File → File transfers…**
- Progress, speed, and ETA live in that window; Open jumps to the saved file
- Folder and reverse preference: **Tools → Options → Files**
- Command line: `/dcc send <nick> <path>`, `/dcc list`, `/dcc cancel`

DCC CHAT is not implemented yet.

Incoming files default to `%AppData%\PureFusionIRC\transfers\`.

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
