# Changelog

All notable changes to PureFusionIRC are listed here. Versions match GitHub Release tags (`v1.0.0-B3`).

## 1.0.0-B3 — 2026-08-16

Third public beta.

### Added
- Theme editor: **View → Theme → Edit theme…**, **Tools → Theme editor…**, and **Options → Edit colors…**. Click any swatch; the main window updates live. Save writes JSON under `%AppData%\PureFusionIRC\themes`

### Fixed
- Join, part, and topic lines no longer light up the mention bar just because they contain your nick
- Built-in theme JSON is no longer overwritten on every startup, so color edits stick

### Notes
- Fonts stay in **Tools → Options**; colors are the new editor
- DCC CHAT, DCC RESUME, and loadable plugin DLLs are still ahead

## 1.0.0-B2 — 2026-08-16

Second public beta.

### Added
- Identd on TCP 113 (on by default). IRCnet-style servers can finish ident during login; bind failure is logged and login still continues. Windows usually needs Administrator for port 113
- Fuller IRCv3: multiline CAP LS, echo-message, server-time, replies (`+draft/reply`), 👍 react (TAGMSG), labeled WHOIS, BATCH multiline, CHATHISTORY when the server offers it, ACCOUNT / CHGHOST / SETNAME, FAIL/WARN/NOTE
- Daily mIRC-style chat logs under `%AppData%\PureFusionIRC\logs`
- Own nick shown mint-green in the nick list

### Fixed
- Auto-join and `/join` accept `c-64` without a leading `#`
- extended-join no longer treats the real name as the channel name
- `userhost-in-names` nicks are stripped to the bare nick

### Notes
- GitHub Releases for this tag are published as the latest Release (not hidden as prerelease)
- DCC CHAT, DCC RESUME, and loadable plugin DLLs are still ahead

## 1.0.0-B1 — 2026-08-16

First public beta. Windows IRC client with an mIRC-style layout, AMOLED-black default theme, and an Inno Setup installer.

### Added
- Connect over TCP or TLS, CAP, SASL PLAIN, CTCP, and a command list (`/join`, `/msg`, `/theme`, `/autojoin`, `/dcc`, …)
- Channel and query buffers, nick list, mIRC color codes, clickable URLs in the default browser
- `@` nick picker in the input box and right-click Reply / Query / Whois on chat lines
- Auto-connect, auto-join, reconnect, and failover to the next listed server after five failures
- Tray icon, optional minimize/close-to-tray, and highlight/query balloons
- Reverse-first DCC file send/receive: Save/Decline prompt, transfers window with progress and speed
- JavaScript scripts (`.pf.js`), settings export/import, JSON themes
- Bluish navy/cyan chrome on buttons, menus, and right-click menus (chat stays dark)
- Help → What's new (this file) and Help → Check for updates (GitHub Releases installer)

### Notes
- DCC CHAT and RESUME are not in this build
- Loadable plugin DLLs are still ahead
