using Dalamud.Configuration;

namespace EmoteIntent;

/// <summary>The player's settings, saved by Dalamud between sessions.</summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    /// <summary>The settings' format; raise it, and migrate, when a setting's meaning changes.</summary>
    public int Version { get; set; }

    /// <summary>
    /// After an emote plays, say which one in chat ("Played /cheer (Cheer)."): the game's own line ("You cheer
    /// enthusiastically.") doesn't name the command. On by default; saved settings without it read as on.
    /// </summary>
    public bool ShowPlayedEmote { get; set; } = true;

    /// <summary>
    /// List every other emote that might fit after the one that played, not only close ones ("Also fits: …"). Off by
    /// default.
    /// </summary>
    public bool ShowAllAlternatives { get; set; }
}
