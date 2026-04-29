# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [1.0.0] — 2026-04-29

### Added

- Fetch Happ-style subscriptions (sends `User-Agent: Happ/2.0` so the server returns the JSON config bundle).
- Build standard `vless://` URIs from XRay outbounds. Supports TCP, Reality, TLS, WebSocket, gRPC.
- Fallback to base64-encoded and plain-text subscription formats.
- Multi-select server list with `Copy links`, `Save links…`, `Save configs…` — operate on selection or the full list.
- Optional `Show JSON Config` panel showing the XRay config for the selected entry; remarks decoded to readable form.
- Remember last 10 URLs (toggleable via `Remember URLs`).
- Persist window size, position, and maximized state in `settings.json` next to the executable.
- Native Windows 10/11 dark title bar.

### Security & robustness

- 10 MB response size cap on subscription fetch.
- http/https-only URL scheme whitelist.
- Atomic settings write (`.tmp` → `File.Move`).
- Per-entry try/catch in JSON parser — one malformed config doesn't break the rest.
- `Process.Start` in About uses scheme whitelist + `UseShellExecute=true` only for vetted URLs.
- `Clipboard.SetText` wrapped — external clipboard locks no longer crash the app.
- Stricter window geometry validation (title bar must be on a working area).
