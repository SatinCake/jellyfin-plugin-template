using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Broadcast.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Broadcast.Scheduling
{
    /// <summary>
    /// Manages creation and retrieval of the broadcast schedule based on plugin configuration.
    /// </summary>
    public class ScheduleManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly object _sync = new();
        private List<BroadcastItem> _schedule = new();
        private static readonly Random Rng = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleManager"/> class.
        /// </summary>
        /// <param name="libraryManager">The Jellyfin library manager used to query items.</param>
        /// <param name="logger">The logger instance.</param>
        public ScheduleManager(ILibraryManager libraryManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current in-memory schedule.
        /// </summary>
        /// <returns>A read-only list of <see cref="BroadcastItem"/> entries.</returns>
        public IReadOnlyList<BroadcastItem> GetCurrentSchedule()
        {
            lock (_sync)
            {
                return _schedule.AsReadOnly();
            }
        }

        /// <summary>
        /// Generates a new 24-hour schedule using the supplied configuration.
        /// </summary>
        /// <param name="config">The plugin configuration to drive schedule generation.</param>
        public void GenerateSchedule(PluginConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);

            if (config.EpisodeDuration <= 0)
            {
                config.EpisodeDuration = 30;
            }

            if (config.CommercialFrequency <= 0)
            {
                config.CommercialFrequency = 3;
            }

            if (config.ScheduleHours <= 0)
            {
                config.ScheduleHours = 24;
            }

            _logger.LogInformation("Generating new broadcast schedule.");

            var mediaItems = GetMediaItems(config.EnabledLibraryIds);
            var commercials = GetCommercials(config.CommercialsFolder);

            if (mediaItems.Count == 0)
            {
                _logger.LogWarning("No media items found in enabled libraries. Cannot generate schedule.");
                _schedule = new List<BroadcastItem>();
                return;
            }

            var newSchedule = new List<BroadcastItem>();
            var now = DateTime.UtcNow;
            DateTime currentTime;
            if (config.AlignToHour)
            {
                currentTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
                // If we're already past exact hour by more than a minute, align to next hour for clean grid
                if (now > currentTime.AddMinutes(1))
                {
                    currentTime = currentTime.AddHours(1);
                }
            }
            else
            {
                currentTime = now.Date; // top of the day UTC
            }

            var endTime = currentTime.AddHours(config.ScheduleHours);
            var showSegmentsPlayed = 0;

            while (currentTime < endTime)
            {
                // Add commercial break
                if (showSegmentsPlayed >= config.CommercialFrequency && commercials.Count > 0)
                {
                    var commercial = commercials[Rng.Next(commercials.Count)];
                    var commercialDuration = commercial.RunTimeTicks.HasValue ? TimeSpan.FromTicks(commercial.RunTimeTicks.Value) : TimeSpan.FromMinutes(1);
                    newSchedule.Add(new BroadcastItem(commercial, currentTime, currentTime + commercialDuration));
                    currentTime += commercialDuration;
                    showSegmentsPlayed = 0;
                    continue;
                }

                // Add media item
                var media = mediaItems[Rng.Next(mediaItems.Count)];
                var duration = TimeSpan.FromMinutes(config.EpisodeDuration);

                // If it's a movie, use its full runtime unless it's shorter
                if (media.RunTimeTicks.HasValue)
                {
                    var mediaRuntime = TimeSpan.FromTicks(media.RunTimeTicks.Value);
                    if (mediaRuntime < duration)
                    {
                        duration = mediaRuntime;
                    }
                }

                newSchedule.Add(new BroadcastItem(media, currentTime, currentTime + duration));
                currentTime += duration;
                showSegmentsPlayed++;
            }

            lock (_sync)
            {
                _schedule = newSchedule;
            }

            _logger.LogInformation("New broadcast schedule generated with {Count} items.", newSchedule.Count);
        }

        /// <summary>
        /// Regenerates the schedule with the given configuration.
        /// </summary>
        /// <param name="config">The plugin configuration.</param>
        public void RegenerateSchedule(PluginConfiguration config) => GenerateSchedule(config);

        /// <summary>
        /// Regenerates the schedule with the given configuration for a specific user context.
        /// </summary>
        /// <param name="config">The plugin configuration.</param>
        /// <param name="user">The user context (optional).</param>
        public void RegenerateSchedule(PluginConfiguration config, object? user) => GenerateSchedule(config);

        private List<BaseItem> GetMediaItems(IEnumerable<string> libraryIds)
        {
            var items = new List<BaseItem>();

            var libs = (libraryIds ?? Array.Empty<string>()).ToList();
            if (libs.Count == 0)
            {
                // Fallback: use all top-level folders
                var root = _libraryManager.GetUserRootFolder();
                libs = root.Children.OfType<Folder>().Select(f => f.Id.ToString("N")).ToList();
                _logger.LogInformation("EnabledLibraryIds empty; falling back to {Count} root libraries.", libs.Count);
            }

            foreach (var libraryId in libs)
            {
                if (!Guid.TryParse(libraryId, out var libraryGuid))
                {
                    _logger.LogWarning("Skipping invalid library id: {LibraryId}", libraryId);
                    continue;
                }

                var library = _libraryManager.GetItemById(libraryGuid);
                Guid ancestorId;
                string ancestorName;
                if (library is Folder folder)
                {
                    ancestorId = folder.Id;
                    ancestorName = folder.Name;
                }
                else if (library is BaseItem bi)
                {
                    ancestorId = bi.Id;
                    ancestorName = bi.Name;
                }
                else
                {
                    _logger.LogWarning("Item with id {Id} could not be resolved to a BaseItem.", libraryGuid);
                    continue;
                }

                var beforeCount = items.Count;
                var query = new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Episode, BaseItemKind.Movie, BaseItemKind.Video, BaseItemKind.MusicVideo },
                    Recursive = true,
                    AncestorIds = new[] { ancestorId }
                };

                var result = _libraryManager.QueryItems(query);
                if (result?.Items != null && result.Items.Count > 0)
                {
                    items.AddRange(result.Items);
                }

                _logger.LogDebug("Ancestor '{Name}' ({Id}) contributed {Delta} items.", ancestorName, ancestorId, items.Count - beforeCount);
            }

            _logger.LogInformation("Total media items found across enabled libraries: {Count}", items.Count);
            return items;
        }

        private List<BaseItem> GetCommercials(string commercialsFolder)
        {
            if (string.IsNullOrWhiteSpace(commercialsFolder) || !Directory.Exists(commercialsFolder))
            {
                _logger.LogDebug("Commercials folder is not configured or does not exist.");
                return new List<BaseItem>();
            }

            var query = new InternalItemsQuery
            {
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Video },
                Path = commercialsFolder
            };

            var items = _libraryManager.QueryItems(query).Items.ToList();
            _logger.LogDebug("Found {Count} commercials.", items.Count);
            return items;
        }
    }
}
