using Dalamud.Configuration;

namespace EmoteIntent;

/// <summary>The player's settings, saved by Dalamud between sessions.</summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    /// <summary>The settings format this version writes. 1: the simplified settings and their defaults.</summary>
    public const int CurrentVersion = 1;

    /// <summary>The settings' format; raise it, and migrate in <see cref="Migrate"/>, when a setting changes.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// When /feel finds an emote: false plays it (the default); true never plays by itself but lists the matches as
    /// links, even a single one, for the player to click.
    /// </summary>
    public bool LetMePick { get; set; }

    /// <summary>Also list, as links, the other emotes that might fit ("Also fits: …"). On by default.</summary>
    public bool ShowAlternatives { get; set; } = true;

    /// <summary>
    /// After /feel played an emote by itself, say which one ("Played /cheer (Cheer)."): the game's own line doesn't
    /// name the command. Never after a click: the player chose it. Off by default.
    /// </summary>
    public bool ShowPlayedEmote { get; set; }

    /// <summary>Brings settings saved by an older version up to date; true when something changed and needs saving.</summary>
    public bool Migrate()
    {
        if (Version >= CurrentVersion)
        {
            return false;
        }

        // Version 0 (0.10–0.11) had other settings and defaults: start from the new defaults.
        LetMePick = false;
        ShowAlternatives = true;
        ShowPlayedEmote = false;
        Version = CurrentVersion;
        return true;
    }
}
