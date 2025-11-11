# Broadcast Jellyfin Plugin

> Continuous TV-style channel generator for Jellyfin with optional commercials and IPTV (M3U + XMLTV) integration.

[![License: GPLv3](https://img.shields.io/badge/License-GPLv3-blue.svg)](#license) [![Jellyfin ABI](https://img.shields.io/badge/ABI-10.9.0.0-orange.svg)](#plugin-manifest)

## Quick Links
- Live TV Setup: use IPTV endpoints below (M3U + XMLTV)
- Diagnostics Endpoints: `/Broadcast/Diagnostics/*`
- IPTV Playlist: `/Broadcast/iptv/playlist.m3u`
- XMLTV EPG: `/Broadcast/iptv/epg.xml`

## Features
- Generate configurable schedule (hours + optional hour alignment)
- Insert commercials from a folder at configurable frequency
- Uses poster thumbnails in schedule + EPG
- Exposes IPTV playlist and XMLTV for Live TV channel
- Now/Next endpoint for fast UI status
- Rich diagnostics endpoints for libraries, schedule and commercials

## Installation (Release Binary)
1. Download latest release asset `Jellyfin.Plugin.Broadcast.dll` from GitHub Releases.
2. Place into: `~/.local/share/jellyfin/plugins/Broadcast/` (create folder if needed).
3. Restart Jellyfin server.
4. Configure under Dashboard -> Plugins -> Broadcast.

## Building From Source
```bash
# Clone
git clone https://github.com/satincake/jellyfin-plugin-broadcast.git
cd jellyfin-plugin-broadcast/Jellyfin.Plugin.Broadcast
# Build
dotnet build -c Release
# Copy to plugins directory
mkdir -p ~/.local/share/jellyfin/plugins/Broadcast
cp bin/Release/net8.0/Jellyfin.Plugin.Broadcast.* ~/.local/share/jellyfin/plugins/Broadcast/
```

## Plugin Manifest
The `build.yaml` file is used by Jellyfin's plugin loader and distribution tooling.
Current values:
- name: Broadcast
- guid: eb5d7894-8eef-4b36-aa6f-5d124e828ce1
- version: 0.1.0.0
- targetAbi: 10.9.0.0
- artifacts: Jellyfin.Plugin.Broadcast.dll

Update `version` and `changelog` when cutting a new release tag.

## Configuration Overview
See in-app page for full detail.
- Enabled Libraries: source media (auto-fallback to all top-level if empty)
- Episode Duration: segment length (minutes)
- Commercial Frequency: segments between ad breaks
- Commercials Folder: ads pool; optional
- ChannelName / ChannelGroup / ChannelId: IPTV identifiers
- ChannelLogoUrl or UseFirstItemAsLogo
- ScheduleHours / AlignToHour

## Endpoints Summary
| Endpoint | Purpose |
|----------|---------|
| GET /Broadcast/Schedule | Full current schedule entries |
| POST /Broadcast/Schedule/Regenerate | Force rebuild schedule |
| GET /Broadcast/NowNext | Current & next item summary |
| GET /Broadcast/Channels | Channel metadata (logo & URLs) |
| GET /Broadcast/iptv/playlist.m3u | IPTV playlist |
| GET /Broadcast/iptv/epg.xml | XMLTV EPG |
| GET /Broadcast/iptv/stream?channelId=... | Stream current item |
| GET /Broadcast/Diagnostics/Config | Config snapshot |
| GET /Broadcast/Diagnostics/Libraries | Library list |
| GET /Broadcast/Diagnostics/LibraryItems | Sample items from library |
| GET /Broadcast/Diagnostics/Schedule | Coverage stats |
| GET /Broadcast/Diagnostics/Commercials | Commercial inventory |

## Release Process
1. Bump `version` + `changelog` in `build.yaml`.
2. Commit and tag: `git tag v0.1.1 && git push --tags`.
3. Create GitHub Release; attach built DLL.
4. Update README if feature changes.

## Contributing
See `CONTRIBUTING.md` for development workflow, guidelines, and testing checklist.

## Code of Conduct
See `CODE_OF_CONDUCT.md`.

## License
GPLv3. By using or contributing you agree to the terms in `LICENSE`.
