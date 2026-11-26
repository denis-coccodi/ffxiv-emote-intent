using Dalamud.Configuration;

namespace EmoteIntent;

/// <summary>The player's settings, saved by Dalamud between sessions.</summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    /// <summary>The settings format this version writes. 2: one "when /feel finds an emote" choice.</summary>
    public const int CurrentVersion = 2;

    /// <summary>The settings' format; raise it, and migrate in <see cref="Migrate"/>, when a setting changes.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>What /feel does once it has found the best emote. Asks when several fit, by default.</summary>
    public WhenFound WhenFound { get; set; } = WhenFound.AskWhenSeveral;

    /// <summary>
    /// After /feel played an emote by itself, say which one ("Played /cheer (Cheer)."): the game's own line doesn't
    /// name the command. Never after a click: the player chose it. Off by default.
    /// </summary>
    public bool ShowPlayedEmote { get; set; }

    /// <summary>
    /// When the phrase bank isn't sure, ask a model running in Ollama on this PC what the text means (AiFallback).
    /// Off by default: it needs Ollama installed and the model downloaded.
    /// </summary>
    public bool UseAi { get; set; }

    /// <summary>The Ollama model asked: gemma3:4b did best of the small free models tried (DEVELOPMENT.md, AI evaluation).</summary>
    public string AiModel { get; set; } = DefaultAiModel;

    /// <summary>The model suggested until the player picks another.</summary>
    public const string DefaultAiModel = "gemma3:4b";

    /// <summary>Brings settings saved by an older version up to date; true when something changed and needs saving.</summary>
    public bool Migrate()
    {
        if (Version >= CurrentVersion)
        {
            return false;
        }

        // Versions 0-1 (0.10–0.12) had other settings: start from the defaults.
        WhenFound = WhenFound.AskWhenSeveral;
        ShowPlayedEmote = false;
        Version = CurrentVersion;
        return true;
    }
}
