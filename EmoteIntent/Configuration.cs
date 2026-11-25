using Dalamud.Configuration;

namespace EmoteIntent;

/// <summary>The player's settings, saved by Dalamud between sessions.</summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    /// <summary>The settings' format; raise it, and migrate, when a setting's meaning changes.</summary>
    public int Version { get; set; }

    /// <summary>
    /// Before every emote /feel plays, print which one and the other emotes that might fit, as links. Off by default:
    /// then only close alternatives are listed, after the emote ("Also fits: …").
    /// </summary>
    public bool AlwaysShowChoice { get; set; }

    /// <summary>
    /// With <see cref="AlwaysShowChoice"/> on: whether the line also says which emote played ("Playing /read (Read).").
    /// Off: only the others are listed ("Others: …"), and nothing when there are none. On by default; saved settings
    /// without it read as on.
    /// </summary>
    public bool ShowPlayedEmote { get; set; } = true;

    /// <summary>
    /// Whether the emote's own line shows in the chat log ("You hug Avar."), as the game does. Off: emotes play as
    /// motion only, unless "text" ends the /feel. On by default; saved settings without it read as on.
    /// </summary>
    public bool ShowChatLine { get; set; } = true;
}
