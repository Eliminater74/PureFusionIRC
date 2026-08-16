# Roadmap

PureFusionIRC is built in layers. **v1.0.0-B3** is a usable Windows IRC client with identd, IRCv3, DCC SEND, logs, a theme editor, installer, and updates. Later milestones fill in power-user mIRC/HexChat features without copying their source.

Living bugs and leftover UI are also in [TODO.md](TODO.md).

## Milestone 0 — Foundation

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

- [ ] Channel list (`/list`) UI, ban list editor, notify/friends list
- [x] Ident server (port 113) for networks that still want it
- [x] NickServ IDENTIFY + SASL PLAIN (EXTERNAL / SCRAM still later)
- [x] Reconnect, backoff, failover to the next listed server
- [x] Logging to disk (mIRC-style daily logs)
- [ ] Find in buffer / search logs
- [ ] Switchbar tabs in addition to the tree (mIRC can do both)
- [x] Clickable URLs (open in the default browser)
- [x] Tray icon, minimize/close-to-tray, highlight/query balloons
- [ ] Spell-as-you-type
- [ ] Sound alerts and taskbar flash
- [ ] Per-network encodings and a dedicated channel-key dialog (keys already work in auto-join `chan key`)

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
- [x] Theme manager / editor (live preview, save JSON under AppData)
- Signed script/plugin directory (optional, out of tree)

## Milestone 4 — Power client

- [x] IRCv3 pieces that help users (chathistory, multiline, react, reply, echo-message, labeled WHOIS, batches)
- bouncer (ZNC/Soju) niceties
- Multi-window / pop-out buffers (mIRC window habit)
- Portable mode (settings beside the EXE)
- [x] Installer (Inno) and GitHub Releases
- [x] In-app update check (download latest setup.exe)
- Accessibility pass (high contrast, screen readers)

## Non-goals

- Not a Linux/macOS port (Windows C# / WPF on purpose, like mIRC)
- Not a HexChat GTK clone or a source port
- Not mIRC script compatibility — PureFusion scripts are a different language by design
