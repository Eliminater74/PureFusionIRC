# TODO

Living list for the next review after core functionality. Strike items as they land; keep ROADMAP.md as the milestone view.

Current release: **v1.0.0-B3**. README.md is the user-facing feature list.

## Protocol

- [x] Identd listener for networks that still fingerprint ident (TCP 113)
- [ ] SASL SCRAM-SHA-256 and EXTERNAL
- [x] DCC SEND (reverse-first) + incoming prompt + transfer window
- [x] In-app update check + changelog + installer download
- [ ] DCC CHAT
- [ ] DCC RESUME / ACCEPT (partial-file UX)
- [x] Chathistory, batch, labeled-response, multiline, react, reply, echo-message (IRCv3)
- [ ] `sts` (strict transport) policy cache
- [ ] Outgoing flood queue with user-visible lag meter beyond RTT ping
- [ ] Message splitting that respects `LINELEN` / UTF-8 code points
- [ ] `/list` filter UI and server list window

## UI

- [ ] Banlist, invite list, exception list windows
- [ ] Channel properties / modes dialog
- [ ] Network list closer to HexChat’s editor (multiple servers per net, cycle)
- [ ] mIRC-style switchbar across the top in addition to the tree
- [ ] Pop-out buffer windows
- [ ] Find in buffer, copy-as-plain vs copy-with-codes
- [x] Font family and size in Options (not yet per-theme)
- [ ] DPI scaling audit
- [x] Tray minimize, close-to-tray, highlight/query balloons
- [ ] Taskbar flash on highlight (`FlashOnHighlight` setting exists, not wired)
- [x] Clickable URLs open in the default browser
- [x] `@` nick picker and Tab nick-complete
- [ ] Keyboard chart (Alt+num switch, Ctrl+B/U/I/K insert codes)

## Theming and settings

- [x] Theme editor (live preview, duplicate, reset, save JSON under AppData)
- [x] Built-in theme JSON is not overwritten on every startup
- [ ] Theme pack file (`.pftheme` JSON + optional background)
- [ ] More factories: High Contrast, Solarized, “Almond” warm-dark if wanted
- [ ] Color-code inserter (mIRC Ctrl+K popover)
- [ ] Portable mode (`PureFusionIRC.exe --portable`)
- [ ] First-run wizard

## Scripts / plugins

- [ ] Timer API, menu API, storage API for JavaScript
- [ ] Script manager window (enable/disable, errors)
- [ ] `IPureFusionPlugin` DLL load from `%AppData%\PureFusionIRC\plugins`
- [ ] Example plugin project in `samples/`

## Quality

- [ ] Broader parser/regression tests (malformed lines, huge tags)
- [ ] UI tests where they pay off
- [x] Chat logs to `%AppData%\PureFusionIRC\logs` (daily per channel)
- [ ] Structured logging (file sink)
- [ ] Crash dump + “copy debug info”
- [ ] Code-signed release binaries

## Docs

- [x] README feature overview for B3 (connect, identd, IRCv3, themes, DCC, logs, updates)
- [ ] Dedicated user manual beyond README
- [ ] Script API reference
- [ ] Screenshot set for GitHub
