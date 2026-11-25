using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
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
        ImGui.TextUnformatted("When /feel finds an emote:");
        if (ImGui.RadioButton("Play it", !configuration.LetMePick))
        {
            Set(() => configuration.LetMePick = false);
        }

        if (ImGui.RadioButton("Let me pick (list the matches as links, even just one)", configuration.LetMePick))
        {
            Set(() => configuration.LetMePick = true);
        }

        ImGui.Spacing();
        var showAlternatives = configuration.ShowAlternatives;
        if (ImGui.Checkbox("Also list other emotes that might fit", ref showAlternatives))
        {
            Set(() => configuration.ShowAlternatives = showAlternatives);
        }

        // Only when /feel plays by itself: when the player picks, they already know which one played.
        using (ImRaii.Disabled(configuration.LetMePick))
        {
            var showPlayed = configuration.ShowPlayedEmote;
            if (ImGui.Checkbox("Say which emote played", ref showPlayed))
            {
                Set(() => configuration.ShowPlayedEmote = showPlayed);
            }

            ImGui.TextWrapped("\"Played /cheer (Cheer).\": the game's own line doesn't name the command.");
        }
    }

    private void Set(Action change)
    {
        change();
        save();
    }
}
