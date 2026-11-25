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
        Choice(WhenFound.PlayBest, "Play the best match", "then list the others that might fit as links");
        Choice(WhenFound.AskWhenSeveral, "Ask when several fit", "play it if it's the only one; otherwise list them all as links");
        Choice(WhenFound.AlwaysPick, "Always let me pick", "list the matches as links, even just one");

        ImGui.Spacing();

        // Only when /feel plays by itself: when the player picks, they already know which one played.
        using (ImRaii.Disabled(configuration.WhenFound == WhenFound.AlwaysPick))
        {
            var showPlayed = configuration.ShowPlayedEmote;
            if (ImGui.Checkbox("Say which emote played", ref showPlayed))
            {
                Set(() => configuration.ShowPlayedEmote = showPlayed);
            }

            ImGui.TextWrapped("\"Played /cheer (Cheer).\": the game's own line doesn't name the command.");
        }
    }

    private void Choice(WhenFound value, string label, string detail)
    {
        if (ImGui.RadioButton(label, configuration.WhenFound == value))
        {
            Set(() => configuration.WhenFound = value);
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"({detail})");
    }

    private void Set(Action change)
    {
        change();
        save();
    }
}
