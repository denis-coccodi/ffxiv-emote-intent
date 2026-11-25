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
}
