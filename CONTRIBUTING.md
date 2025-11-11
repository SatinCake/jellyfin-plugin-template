# Contributing to Broadcast

Thanks for your interest in contributing!

## Development Setup
1. Clone this repository alongside `jellyfin` (and optionally `jellyfin-web`).
2. Build once: `dotnet build Jellyfin.Plugin.Broadcast/Jellyfin.Plugin.Broadcast.csproj -c Debug`.
3. Copy DLL & PDB to your Jellyfin data dir under `plugins/Broadcast/`.
4. Restart Jellyfin; plugin appears under Dashboard -> Plugins.
5. Use the provided README debugging section for VS Code automation.

## Branching & Releases
- `main`: active development.
- Version tags follow `vMAJOR.MINOR.PATCH`.
- Update `build.yaml` version and changelog section when preparing a release.

## Code Style & Quality
- C# 12 / net8.0.
- All analyzer warnings are treated as errors.
- StyleCop ruleset (`jellyfin.ruleset`) enforced.
- Keep edits minimal and focused; avoid large refactors in feature PRs.

## Commit Messages
Format (recommended):
```
feat: add XMLTV programme icons
fix: handle empty library gracefully
chore: update build metadata
```

## Testing Checklist for PRs
- Build succeeds locally.
- New config fields have sane defaults (no breaking serialization changes).
- Schedule generation still produces entries.
- IPTV endpoints (playlist.m3u, epg.xml) respond 200.
- Diagnostics endpoints return JSON.

## Adding Features
1. Open a feature request issue (or reference an existing one).
2. Discuss approach if large in scope.
3. Implement with incremental commits.
4. Add/update documentation in README where relevant.

## Reporting Bugs
Provide:
- Jellyfin server version
- Plugin version
- Logs + output from: `/Broadcast/Diagnostics/Config`, `/Broadcast/Diagnostics/Schedule`
- Reproduction steps

## License
This project is GPLv3. By contributing, you agree that your contributions are licensed under GPLv3.

## Contact
Open an issue for questions or ideas.

