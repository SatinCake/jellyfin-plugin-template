using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Broadcast.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // set default options here
        CommercialsFolder = string.Empty;
        EpisodeDuration = 30;
        CommercialFrequency = 3;
        EnabledLibraryIds = []; // Empty array default

        // Channel defaults
        ChannelName = "Broadcast Channel";
        ChannelGroup = "Broadcast";
        ChannelId = "broadcast-1";
        UseFirstItemAsLogo = true;
        ChannelLogoUrl = string.Empty;

        // Schedule defaults
        ScheduleHours = 24;
        AlignToHour = false;
    }

    /// <summary>
    /// Gets or sets the path to the commercials folder.
    /// </summary>
    public string CommercialsFolder { get; set; }

    /// <summary>
    /// Gets or sets the episode duration in minutes.
    /// </summary>
    public int EpisodeDuration { get; set; }

    /// <summary>
    /// Gets or sets the commercial frequency.
    /// </summary>
    public int CommercialFrequency { get; set; }

    /// <summary>
    /// Gets or sets the enabled library ids. Array form retained for XML serialization compatibility.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Jellyfin XML serializer requires concrete array for persistence.")]
    public string[] EnabledLibraryIds { get; set; }

    /// <summary>
    /// Gets or sets the IPTV channel display name.
    /// </summary>
    public string ChannelName { get; set; }

    /// <summary>
    /// Gets or sets the IPTV channel group.
    /// </summary>
    public string ChannelGroup { get; set; }

    /// <summary>
    /// Gets or sets the IPTV channel id/slug (stable).
    /// </summary>
    public string ChannelId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the first scheduled item image as the channel logo.
    /// </summary>
    public bool UseFirstItemAsLogo { get; set; }

    /// <summary>
    /// Gets or sets a custom channel logo URL. If set, overrides UseFirstItemAsLogo.
    /// </summary>
    public string ChannelLogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the schedule duration in hours.
    /// </summary>
    public int ScheduleHours { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to align schedule start to top of the hour.
    /// </summary>
    public bool AlignToHour { get; set; }
}
