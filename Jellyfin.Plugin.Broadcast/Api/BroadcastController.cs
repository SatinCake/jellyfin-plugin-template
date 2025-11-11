// filepath: /home/satin/Documents/Projects/jellyfin-plugin-template/Jellyfin.Plugin.Broadcast/Api/BroadcastController.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Broadcast.Configuration;
using Jellyfin.Plugin.Broadcast.Scheduling;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Broadcast.Api
{
    /// <summary>
    /// Provides endpoints for interacting with the broadcast schedule.
    /// </summary>
    [ApiController]
    [Route("Broadcast")]
    public class BroadcastController : ControllerBase
    {
        private static ScheduleManager? _scheduleManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BroadcastController"/> class.
        /// </summary>
        /// <param name="libraryManager">Library manager for querying items.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        public BroadcastController(ILibraryManager libraryManager, ILoggerFactory loggerFactory)
        {
            _libraryManager = libraryManager;
            _logger = loggerFactory.CreateLogger("BroadcastController");
            _scheduleManager ??= new ScheduleManager(libraryManager, loggerFactory.CreateLogger("BroadcastSchedule"));
        }

        /// <summary>
        /// Gets the current schedule entries. Generates one if none exist yet.
        /// </summary>
        /// <returns>JSON array of schedule entries with id, name, start/end, and image URL.</returns>
        [HttpGet("Schedule")]
        public IActionResult GetSchedule()
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (_scheduleManager!.GetCurrentSchedule().Count == 0)
            {
                _scheduleManager.RegenerateSchedule(config);
            }

            var schedule = _scheduleManager.GetCurrentSchedule()
                .Select(i => new
                {
                    Id = i.Media.Id,
                    MediaName = i.Media.Name,
                    Start = i.StartTime,
                    End = i.EndTime,
                    ImageUrl = BuildItemImageUrl(i.Media.Id, 240)
                })
                .ToList();
            return Ok(schedule);
        }

        /// <summary>
        /// Returns the current and next programme.
        /// </summary>
        /// <returns>JSON object with Now and Next properties.</returns>
        [HttpGet("NowNext")]
        public IActionResult GetNowNext()
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var sched = _scheduleManager!.GetCurrentSchedule();
            if (sched.Count == 0)
            {
                _scheduleManager.RegenerateSchedule(config);
                sched = _scheduleManager.GetCurrentSchedule();
            }

            var now = DateTime.UtcNow;
            BroadcastItem? current = null;
            BroadcastItem? next = null;

            // Lists are indexable; walk once to find current and next efficiently
            for (int i = 0; i < sched.Count; i++)
            {
                var e = sched[i];
                if (e.StartTime <= now && e.EndTime > now)
                {
                    current = e;
                    if (i + 1 < sched.Count)
                    {
                        next = sched[i + 1];
                    }

                    break;
                }
                else if (e.StartTime > now)
                {
                    // current remains null if not set yet
                    next = e;
                    break;
                }
            }

            var nowObj = current is null ? null : new { current.Media.Id, Name = current.Media.Name, current.StartTime, current.EndTime, ImageUrl = BuildItemImageUrl(current.Media.Id, 320) };
            var nextObj = next is null ? null : new { next.Media.Id, Name = next.Media.Name, next.StartTime, next.EndTime, ImageUrl = BuildItemImageUrl(next.Media.Id, 320) };

            return Ok(new { Now = nowObj, Next = nextObj });
        }

        /// <summary>
        /// Returns channel metadata for IPTV consumers.
        /// </summary>
        /// <returns>JSON array of one channel containing id, name, group, logo, and URLs.</returns>
        [HttpGet("Channels")]
        public IActionResult GetChannels()
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            string logo;
            if (!string.IsNullOrWhiteSpace(cfg.ChannelLogoUrl))
            {
                logo = cfg.ChannelLogoUrl;
            }
            else
            {
                var sched = _scheduleManager!.GetCurrentSchedule();
                if (sched.Count > 0 && sched[0].Media?.Id is Guid mid)
                {
                    logo = BuildItemImageUrl(mid, 320);
                }
                else
                {
                    logo = string.Empty;
                }
            }

            return Ok(new[]
            {
                new
                {
                    Id = cfg.ChannelId,
                    Name = cfg.ChannelName,
                    Group = cfg.ChannelGroup,
                    Logo = logo,
                    Playlist = $"{baseUrl}/Broadcast/iptv/playlist.m3u",
                    Epg = $"{baseUrl}/Broadcast/iptv/epg.xml",
                    Stream = $"{baseUrl}/Broadcast/iptv/stream?channelId={Uri.EscapeDataString(cfg.ChannelId)}"
                }
            });
        }

        /// <summary>
        /// Regenerates the schedule using current configuration.
        /// </summary>
        /// <returns>No content.</returns>
        [HttpPost("Schedule/Regenerate")]
        public IActionResult Regenerate()
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            _scheduleManager!.RegenerateSchedule(config);
            return NoContent();
        }

        /// <summary>
        /// Returns available libraries (top-level folders).
        /// </summary>
        /// <returns>An array of library id/name objects.</returns>
        [HttpGet("Libraries")]
        public IActionResult GetLibraries()
        {
            var root = _libraryManager.GetUserRootFolder();
            var items = root.Children
                .OfType<Folder>()
                .Select(f => new { f.Id, f.Name, Type = f.GetType().Name })
                .ToList();
            return Ok(items);
        }

        /// <summary>
        /// Returns current plugin configuration for diagnostics.
        /// </summary>
        /// <returns>Configuration snapshot.</returns>
        [HttpGet("Diagnostics/Config")]
        public IActionResult GetDiagnosticsConfig()
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            return Ok(new
            {
                config.CommercialsFolder,
                config.EpisodeDuration,
                config.CommercialFrequency,
                EnabledLibraryIds = config.EnabledLibraryIds ?? Array.Empty<string>(),
                EnabledLibraryCount = config.EnabledLibraryIds?.Length ?? 0
            });
        }

        /// <summary>
        /// Lists top-level user libraries with basic metadata for diagnostics.
        /// </summary>
        /// <returns>List of libraries.</returns>
        [HttpGet("Diagnostics/Libraries")]
        public IActionResult GetDiagnosticsLibraries()
        {
            var root = _libraryManager.GetUserRootFolder();
            var items = root.Children
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    Type = c.GetType().Name
                })
                .ToList();
            return Ok(items);
        }

        /// <summary>
        /// Returns item counts and examples under an ancestor (library/view) id.
        /// </summary>
        /// <param name="libraryId">The library/view id.</param>
        /// <param name="sample">Number of sample items to return.</param>
        /// <returns>Counts and samples.</returns>
        [HttpGet("Diagnostics/LibraryItems")]
        public IActionResult GetDiagnosticsLibraryItems([FromQuery] string libraryId, [FromQuery] int sample = 5)
        {
            if (!Guid.TryParse(libraryId, out var id))
            {
                return BadRequest(new { error = "Invalid library id" });
            }

            var ancestor = _libraryManager.GetItemById(id);
            if (ancestor is null)
            {
                return NotFound(new { error = "Ancestor not found" });
            }

            var query = new InternalItemsQuery
            {
                Recursive = true,
                AncestorIds = new[] { ancestor.Id },
                IncludeItemTypes = new[] { BaseItemKind.Episode, BaseItemKind.Movie, BaseItemKind.Video, BaseItemKind.MusicVideo }
            };

            var results = _libraryManager.QueryItems(query);
            var items = results.Items ?? new List<BaseItem>();
            var total = items.Count;

            var byType = items
                .GroupBy(i => i.GetType().Name)
                .ToDictionary(g => g.Key, g => g.Count());

            var samples = items
                .Take(Math.Max(0, sample))
                .Select(i => new { i.Id, i.Name, Type = i.GetType().Name })
                .ToList();

            return Ok(new
            {
                Ancestor = new { ancestor.Id, ancestor.Name, Type = ancestor.GetType().Name },
                Total = total,
                ByType = byType,
                Samples = samples
            });
        }

        /// <summary>
        /// Returns diagnostics for commercials discovery based on configuration.
        /// </summary>
        /// <returns>Commercials summary.</returns>
        [HttpGet("Diagnostics/Commercials")]
        public IActionResult GetDiagnosticsCommercials()
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var folder = config.CommercialsFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                return Ok(new { Configured = false, Count = 0, Samples = Array.Empty<object>() });
            }

            if (!Directory.Exists(folder))
            {
                return Ok(new { Configured = true, Exists = false, Path = folder, Count = 0, Samples = Array.Empty<object>() });
            }

            var query = new InternalItemsQuery
            {
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Video },
                Path = folder
            };
            var results = _libraryManager.QueryItems(query);
            var items = results.Items ?? new List<BaseItem>();
            var samples = items.Take(5).Select(i => new { i.Id, i.Name, Type = i.GetType().Name }).ToList();

            return Ok(new { Configured = true, Exists = true, Path = folder, Count = items.Count, Samples = samples });
        }

        /// <summary>
        /// Returns schedule diagnostics including counts and coverage.
        /// </summary>
        /// <returns>Schedule diagnostics.</returns>
        [HttpGet("Diagnostics/Schedule")]
        public IActionResult GetDiagnosticsSchedule()
        {
            var entries = _scheduleManager!.GetCurrentSchedule();
            var count = entries.Count;
            if (count == 0)
            {
                return Ok(new { Count = 0, CoverageHours = 0, Earliest = (DateTime?)null, Latest = (DateTime?)null });
            }

            var earliest = entries.Min(e => e.StartTime);
            var latest = entries.Max(e => e.EndTime);
            var coverage = (latest - earliest).TotalHours;
            var samples = entries.Take(5).Select(e => new { e.Media.Name, e.StartTime, e.EndTime }).ToList();

            return Ok(new { Count = count, CoverageHours = coverage, Earliest = earliest, Latest = latest, Samples = samples });
        }

        /// <summary>
        /// IPTV playlist (M3U) for adding this plugin as an IPTV tuner.
        /// </summary>
        /// <returns>M3U playlist text.</returns>
        [HttpGet("iptv/playlist.m3u")]
        public IActionResult GetIptvPlaylist()
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var channelId = cfg.ChannelId;
            var tvgId = channelId;
            var tvgName = cfg.ChannelName;
            var group = cfg.ChannelGroup;
            var epgUrl = $"{baseUrl}/Broadcast/iptv/epg.xml";

            string logo;
            if (!string.IsNullOrWhiteSpace(cfg.ChannelLogoUrl))
            {
                logo = cfg.ChannelLogoUrl;
            }
            else if (cfg.UseFirstItemAsLogo)
            {
                var entries = _scheduleManager!.GetCurrentSchedule();
                Guid? logoId = entries.Count > 0 ? entries[0].Media?.Id : null;
                logo = logoId.HasValue ? BuildItemImageUrl(logoId.Value, 320) : string.Empty;
            }
            else
            {
                logo = string.Empty;
            }

            var streamUrl = $"{baseUrl}/Broadcast/iptv/stream?channelId={Uri.EscapeDataString(channelId)}";

            var lines = new List<string>
            {
                "#EXTM3U",
                $"#EXTINF:-1 tvg-id=\"{tvgId}\" tvg-name=\"{tvgName}\" tvg-logo=\"{logo}\" tvg-url=\"{epgUrl}\" group-title=\"{group}\",{tvgName}",
                streamUrl
            };
            var m3u = string.Join('\n', lines);
            return Content(m3u, "application/x-mpegURL");
        }

        /// <summary>
        /// XMLTV EPG for IPTV tuner.
        /// </summary>
        /// <returns>XMLTV document.</returns>
        [HttpGet("iptv/epg.xml")]
        public IActionResult GetXmlTv()
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var channelId = cfg.ChannelId;
            var channelName = cfg.ChannelName;
            var entries = _scheduleManager!.GetCurrentSchedule();
            if (entries.Count == 0)
            {
                _scheduleManager.RegenerateSchedule(cfg);
                entries = _scheduleManager.GetCurrentSchedule();
            }

            string channelIcon;
            if (!string.IsNullOrWhiteSpace(cfg.ChannelLogoUrl))
            {
                channelIcon = cfg.ChannelLogoUrl;
            }
            else if (cfg.UseFirstItemAsLogo)
            {
                channelIcon = entries.Count > 0 && entries[0].Media?.Id is Guid mid ? BuildItemImageUrl(mid, 320) : string.Empty;
            }
            else
            {
                channelIcon = string.Empty;
            }

            using var ms = new MemoryStream();
            using (var writer = new StreamWriter(ms, System.Text.Encoding.UTF8, 1024, leaveOpen: true))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<tv generator-info-name=\"Broadcast Plugin\">\n");
                writer.WriteLine($"  <channel id=\"{channelId}\">\n    <display-name>{System.Security.SecurityElement.Escape(channelName)}</display-name>");
                if (!string.IsNullOrEmpty(channelIcon))
                {
                    writer.WriteLine($"    <icon src=\"{channelIcon}\"/>");
                }

                writer.WriteLine("  </channel>");

                foreach (var e in entries)
                {
                    var start = e.StartTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + " +0000";
                    var stop = e.EndTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + " +0000";
                    var title = e.Media?.Name ?? "Program";
                    var icon = e.Media?.Id is Guid mid ? BuildItemImageUrl(mid, 320) : string.Empty;
                    writer.WriteLine($"  <programme start=\"{start}\" stop=\"{stop}\" channel=\"{channelId}\">");
                    writer.WriteLine($"    <title lang=\"en\">{System.Security.SecurityElement.Escape(title)}</title>");
                    if (!string.IsNullOrEmpty(icon))
                    {
                        writer.WriteLine($"    <icon src=\"{icon}\"/>");
                    }

                    writer.WriteLine("  </programme>");
                }

                writer.WriteLine("\n</tv>");
            }

            ms.Position = 0;
            return File(ms.ToArray(), "application/xml");
        }

        /// <summary>
        /// Streams the currently scheduled media for the given channel id.
        /// </summary>
        /// <param name="channelId">The channel identifier (currently 'broadcast-1').</param>
        /// <returns>Stream result.</returns>
        [HttpGet("iptv/stream")]
        public IActionResult StreamChannel([FromQuery] string channelId)
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!string.Equals(channelId, cfg.ChannelId, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var now = DateTime.UtcNow;
            var entries = _scheduleManager!.GetCurrentSchedule();
            if (entries.Count == 0)
            {
                var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
                _scheduleManager.RegenerateSchedule(config);
                entries = _scheduleManager.GetCurrentSchedule();
            }

            var current = entries
                .OrderBy(e => e.StartTime)
                .FirstOrDefault(e => e.StartTime <= now && e.EndTime > now);

            if (current is null && entries.Count > 0)
            {
                current = entries[0];
            }

            if (current?.Media is null)
            {
                return NotFound();
            }

            var path = current.Media.Path;
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                _logger.LogWarning("Scheduled media path not found: {Path}", path);
                return NotFound();
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        private string BuildItemImageUrl(Guid itemId, int width)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return $"{baseUrl}/Items/{itemId}/Images/Primary?fillWidth={width}";
        }
    }
}
