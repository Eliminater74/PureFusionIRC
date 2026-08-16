# Roadmap

PureFusionIRC is built in layers. Core (this milestone) is a usable Windows IRC client. Later milestones fill in power-user mIRC/HexChat features without copying their source.

## Milestone 0 — Foundation (this pass)

- [x] Repository ignore rules (`TEMP/`, build outputs, secrets)
- [x] C# solution: Core library, WPF app, tests
- [x] README, ROADMAP, TODO
- [x] Theme engine with AMOLED Black default, Classic Light, Charcoal
- [x] IRC TCP + TLS, parser, CAP, SASL PLAIN, ping/lag
- [x] Channel/query buffers, nick list, mIRC color codes
- [x] Command processor and input history
- [x] Settings store + export/import pack
- [x] JavaScript script host (not mSL, not HexChat Python)

## Milestone 1 — Daily driver

- Channel list (`/list`) UI, ban list editor, notify/friends list
- Ident server (port 113) for networks that still want it
- NickServ helper + SASL EXTERNAL / SCRAM when needed
- Per-network encodings, reconnect/backoff polish, channel keys UI
- Logging to disk (mIRC-style logs) with search
- Switchbar tabs in addition to the tree (mIRC can do both)
- Spell-as-you-type optional, better URL/click handling
- Sound/highlight rules, tray icon, balloon/toasts

## Milestone 2 — File transfer and extras

- [x] DCC send/receive with reverse-first NAT, incoming prompt, and a transfer window
- DCC chat; RESUME/ACCEPT (continue a partial file)
- CTCP SOUND / AVATAR leftovers only if people still use them
- Ignore list with wildcards, flood protection tunables
- Favorites, perform-on-connect scripts per network
- Raw log window, built-in hex dump for debugging

## Milestone 3 — Scripts and plugins as a platform

- Documented JavaScript API (timers, hooks, menus, storage)
- Loadable plugin assemblies (`IPureFusionPlugin`) with versioned ABI
- Optional extra script languages later (Lua or C# scripts) — still not mSL
- Theme manager window (preview, import `.pftheme`)
- Signed script/plugin directory (optional, out of tree)

## Milestone 4 — Power client

- Full IRCv3 remaining pieces that help users (chathistory, multiline, react, reply)
- bouncer (ZNC/Soju) niceties
- Multi-window / pop-out buffers (mIRC window habit)
- Portable mode (settings beside the EXE)
- [x] Installer (Inno) and GitHub Releases
- [x] In-app update check (download latest setup.exe)
- Accessibility pass (high contrast, screen readers)
- Accessibility pass (high contrast, screen readers)

## Non-goals

- Not a Linux/macOS port (Windows C# / WPF on purpose, like mIRC)
- Not a HexChat GTK clone or a source port
- Not mIRC script compatibility — PureFusion scripts are a different language by design
