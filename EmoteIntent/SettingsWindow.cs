using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace EmoteIntent;

/// <summary>The settings window: opened from the plugin list's gear button or with /feelconfig.</summary>
internal sealed class SettingsWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action save;

    public SettingsWindow(Configuration configuration, Action save)
        : base("Emote Intent settings###EmoteIntentSettings")
    {
        this.configuration = configuration;
        this.save = save;
        Size = new Vector2(420, 0);
        SizeCondition = ImGuiCond.FirstUseEver;
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    public override void Draw()
    {
        var showPlayed = configuration.ShowPlayedEmote;
        if (ImGui.Checkbox("Say which emote /feel played", ref showPlayed))
        {
            configuration.ShowPlayedEmote = showPlayed;
            save();
        }

        ImGui.TextWrapped(
            "After the emote, chat says \"Played /cheer (Cheer).\": the game's own line (\"You cheer "
            + "enthusiastically.\") doesn't name the command.");

        ImGui.Spacing();
        var showAll = configuration.ShowAllAlternatives;
        if (ImGui.Checkbox("List all alternatives, not only close ones", ref showAll))
        {
            configuration.ShowAllAlternatives = showAll;
            save();
        }

        ImGui.TextWrapped(
            "Other emotes that might fit are listed after the one that played, as links (\"Also fits: /carrybook\"). "
            + "Off: only close ones.");
    }
}
