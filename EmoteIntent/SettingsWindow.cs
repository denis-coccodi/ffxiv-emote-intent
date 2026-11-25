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
        var alwaysShow = configuration.AlwaysShowChoice;
        if (ImGui.Checkbox("Always show what /feel plays and the alternatives", ref alwaysShow))
        {
            configuration.AlwaysShowChoice = alwaysShow;
            save();
        }

        ImGui.TextWrapped(
            "Before every emote, a chat line says which one plays and lists the others that might fit, as links. "
            + "Off: only close alternatives are listed, after the emote.");

        // A sub-option of the one above: only meaningful while it's on.
        using (ImRaii.Disabled(!configuration.AlwaysShowChoice))
        {
            ImGui.Indent();
            var showPlayed = configuration.ShowPlayedEmote;
            if (ImGui.Checkbox("Say which emote played", ref showPlayed))
            {
                configuration.ShowPlayedEmote = showPlayed;
                save();
            }

            ImGui.TextWrapped("Off: the line lists only the others, and isn't written when there are none.");
            ImGui.Unindent();
        }

        ImGui.Spacing();
        var showChatLine = configuration.ShowChatLine;
        if (ImGui.Checkbox("Show the emote's line in the chat log", ref showChatLine))
        {
            configuration.ShowChatLine = showChatLine;
            save();
        }

        ImGui.TextWrapped(
            "The line the game writes for an emote, like \"You hug Avar.\". Off: emotes play as motion only; end a "
            + "/feel with \"text\" to show the line that time. On: end it with \"motion\" to hide it that time.");
    }
}
