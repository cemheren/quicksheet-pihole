# quicksheet-pihole

A [QuickSheet](https://github.com/cemheren/QuickSheet) extension that displays [Pi-hole](https://pi-hole.net) DNS blocking stats directly on your desktop wallpaper or terminal spreadsheet.

![Pi-hole stats on wallpaper](https://raw.githubusercontent.com/cemheren/QuickSheet/main/docs/screenshots/desktop-wallpaper-commands.png)

## What it shows

```
Pi-hole: pi.hole
Status:  🟢 ENABLED
Blocked: 19.2%  [███░░░░░░░░░░░░░]
Queries: 8,432 today
Blocked: 1,619  Cached: 2,100
Fwd:     4,713
Clients: 6 active
Domains: 1,512,347 blocked
```

## Install

```
ext: github:cemheren/quicksheet-pihole
```

## Usage

| Cell value | Description |
|---|---|
| `pihole:` | Uses `pi.hole` (mDNS default) |
| `pihole: 192.168.1.100` | Custom Pi-hole IP or hostname |
| `pihole: http://pihole.local` | Full URL |
| `pihole: TOKEN@192.168.1.100` | With API token (for auth-protected instances) |

> **API token**: Settings → API / Web interface → Show API token in Pi-hole admin panel.

## Requirements

- .NET 9 SDK
- Pi-hole v5+ running on your network
- QuickSheet installed

## Protocol

Uses the Pi-hole v5 REST API: `GET /admin/api.php?summaryRaw`. No API key required by default. Stats are cached for 30 seconds to avoid hammering the server.

## Links

- [QuickSheet](https://github.com/cemheren/QuickSheet) — main project
- [Pi-hole](https://pi-hole.net) — DNS-level ad blocking
- [QuickSheet extension protocol](https://github.com/cemheren/QuickSheet/blob/main/docs/extension-protocol.md)
