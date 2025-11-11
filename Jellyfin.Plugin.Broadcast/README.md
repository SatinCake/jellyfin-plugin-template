# Jellyfin Broadcast Mode Plugin

This plugin creates a TV-like experience by generating random schedules of your media content with commercial breaks. It simulates the experience of traditional broadcast television within your Jellyfin server.

## Features

- Creates a TV channel that plays random shows from your libraries
- Automatically generates a 24-hour broadcast schedule
- Inserts commercials at configurable intervals
- Provides an upcoming schedule view with thumbnails
- Configurable episode duration and commercial frequency
- IPTV endpoints (M3U + XMLTV) to integrate with Jellyfin Live TV

## Installation

1. Build or download the plugin.
2. Copy `Jellyfin.Plugin.Broadcast.dll` (and its PDB for debugging) into your Jellyfin data directory under `plugins/Broadcast/`.
3. Restart your Jellyfin server.

## Configuration

1. Go to Dashboard → Plugins → Broadcast
2. Configure:
   - Commercials Folder: path to short videos for ad breaks
   - Episode Duration: minutes per show segment
   - Commercial Frequency: number of segments between ad breaks
   - Include Libraries: choose libraries to draw content from
3. Save. Optionally click Regenerate Broadcast Schedule.
4. The top Status panel shows libraries, item estimates, and schedule coverage. The schedule table shows the first 50 entries with thumbnails.

## Live TV (IPTV) Integration

Use these URLs to add the plugin as an IPTV tuner and guide in Jellyfin:
- M3U playlist: `http(s)://<server>/Broadcast/iptv/playlist.m3u`
- XMLTV EPG: `http(s)://<server>/Broadcast/iptv/epg.xml`
- Stream endpoint: `http(s)://<server>/Broadcast/iptv/stream?channelId=broadcast-1`

Steps:
1. Dashboard → Live TV → Tuners → Add → M3U tuner → use the playlist URL.
2. Live TV → Guide → Add → XMLTV guide → use the EPG URL.
3. Finish setup. The “Broadcast Channel” will appear with the generated schedule.

Notes:
- If no libraries are selected, the plugin falls back to all top-level libraries.
- EPG times are UTC with +0000 offset. Program entries include poster icons.
- Stream endpoint serves the underlying file with range support; Jellyfin may transcode as needed for clients.

## How to Watch (without Live TV)

- Use the Schedule view in the plugin config page to preview upcoming content.
- The stream endpoint can be opened directly if desired.

## Troubleshooting

- No libraries listed: ensure you have libraries configured on the server; click Refresh in the Libraries section.
- Empty schedule: save configuration and click Regenerate; ensure your libraries contain Episodes/Movies/Video items.
- No thumbnails: verify items have a Primary image in Jellyfin; the config page and EPG pull from `/Items/{id}/Images/Primary`.
- Live TV not showing the channel: re-check M3U/EPG URLs and server port; confirm plugin DLL is in `plugins/Broadcast/`.

## Requirements

- Jellyfin 10.8+ (net8.0)
- At least one library with episodes, movies, or videos
- Optional: a folder with short videos for commercials

## License

GPLv3 (for binary distribution alongside Jellyfin GPL libraries).

## Support

Open issues in this repository with logs from:
- GET `/Broadcast/Diagnostics/Config`
- GET `/Broadcast/Diagnostics/Libraries`
- GET `/Broadcast/Diagnostics/Schedule`
- The browser console on the config page (for UI problems)
