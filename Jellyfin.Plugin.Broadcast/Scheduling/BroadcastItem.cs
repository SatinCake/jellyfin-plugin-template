using System;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Broadcast.Scheduling
{
    /// <summary>
    /// Represents a scheduled media item with a start and end time.
    /// </summary>
    public class BroadcastItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BroadcastItem"/> class.
        /// </summary>
        /// <param name="media">The media item.</param>
        /// <param name="startTime">The scheduled start time (UTC).</param>
        /// <param name="endTime">The scheduled end time (UTC).</param>
        public BroadcastItem(BaseItem media, DateTime startTime, DateTime endTime)
        {
            Media = media;
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// Gets the media item.
        /// </summary>
        public BaseItem Media { get; }

        /// <summary>
        /// Gets the scheduled start time.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Gets the scheduled end time.
        /// </summary>
        public DateTime EndTime { get; }
    }
}
