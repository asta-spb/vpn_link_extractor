# VPN Link Extractor

Windows desktop tool that extracts `vless://` links and XRay configs from a Happ-style subscription URL.

WinForms, .NET 10. Dark theme, single window.

## Features

- Fetches Happ-style subscriptions (sends the proper `User-Agent` to receive the JSON config bundle).
- Builds standard `vless://` URIs from XRay outbound configs — supports TCP, Reality, TLS, WebSocket, gRPC.
- Falls back to base64-encoded and plain-text subscription formats.
- Multi-select list of servers with `Copy links` / `Save links…` / `Save configs…` (operates on selection if any, otherwise on the full list).
- Optional `Show JSON Config` panel — inspects the underlying XRay JSON for the selected entry; remarks are decoded to readable form.
- Remembers up to 10 last URLs (toggleable via `Remember URLs`).
- Persists window size, position and maximized state across launches in `settings.json` next to the executable.
- Native dark title bar on Windows 10/11.
- Defensive: 10 MB response cap, http/https-only schemes, atomic settings write, safe JSON parsing per entry.

## Requirements

- Windows 10 1809+ or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build (or .NET 10 Runtime if running a framework-dependent build)

## Build

```bash
dotnet build -c Release
```

Output: `bin/Release/net10.0-windows/VpnLinkExtractor.exe`.

## Publish (single-file `.exe`)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Result: `bin/Release/net10.0-windows/win-x64/publish/VpnLinkExtractor.exe` — single self-contained executable, no .NET runtime required on the target.

For a smaller framework-dependent build use `--self-contained false`.

## Usage

1. Paste a Happ subscription URL (e.g. `https://example.com/connection/subs/<token>`).
2. Press **Fetch**.
3. Select one or more entries (Ctrl/Shift-click) and use **Copy links**, **Save links…** or **Save configs…**.
4. Toggle **Show JSON Config** to view the underlying XRay config for the selected entry.

When **Remember URLs** is on, successfully fetched URLs are saved to the dropdown for next time.

## Project layout

| File | Purpose |
|---|---|
| `Program.cs` | Entry point + `--test`/`--dump-config` CLI debug modes |
| `MainForm.cs` | Main window, all UI and action handlers |
| `AboutForm.cs` | About dialog with project links |
| `SubscriptionFetcher.cs` | HTTP fetch, JSON / base64 / plain-text parsing, VLESS URI builder |
| `VpnEntry.cs` | Result record (remarks, vless URI, config JSON) |
| `AppSettings.cs` | JSON-backed persisted settings |
| `WindowGeometry.cs` | Persisted window state |
| `Theme.cs` | Color palette and fonts |
| `FlatBtn.cs` | Custom themed Button |
| `DarkTitleBar.cs` | DWM dark title bar interop |
| `app.ico` | Application icon (multi-resolution) |

## License

MIT — see [LICENSE](LICENSE).

## Links

- Community: [t.me/nastya_chtoto_delaet](https://t.me/nastya_chtoto_delaet)
- Author: [t.me/anastasia98](https://t.me/anastasia98)
