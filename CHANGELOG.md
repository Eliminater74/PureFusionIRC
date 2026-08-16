# Changelog

All notable changes to PureFusionIRC are listed here. Versions match GitHub Release tags (`v1.0.0-B1`).

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
- Identd, fuller IRCv3, and loadable plugin DLLs are still ahead
