# TODO

Living list for the next review after core functionality. Strike items as they land; keep ROADMAP.md as the milestone view.

## Protocol

- [ ] Identd listener for networks that still fingerprint ident
- [ ] SASL SCRAM-SHA-256 and EXTERNAL
- [x] DCC SEND (reverse-first) + incoming prompt + transfer window
- [x] In-app update check + changelog + installer download
- [ ] DCC CHAT
- [ ] DCC RESUME / ACCEPT (partial-file UX)
- [ ] Chathistory, batch, labeled-response, multiline, react (IRCv3)
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
- [ ] Customizable fonts per theme, DPI scaling audit
- [ ] Tray minimize, highlight flash on taskbar
- [ ] Keyboard chart (Tab nick-complete polish, Alt+num switch, Ctrl+B/U/I/K insert codes)

## Theming and settings

- [ ] Theme preview dialog and `.pftheme` pack (JSON + optional background)
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
- [ ] Structured logging (file sink)
- [ ] Crash dump + “copy debug info”
- [ ] Code-signed release binaries

## Docs

- [ ] User manual (connect, colors, scripts)
- [ ] Script API reference
- [ ] Screenshot set for GitHub
