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
        var alwaysShow = configuration.AlwaysShowChoice;
        if (ImGui.Checkbox("Always show what /feel plays and the alternatives", ref alwaysShow))
        {
            configuration.AlwaysShowChoice = alwaysShow;
            save();
        }

        ImGui.TextWrapped(
            "Before every emote, a chat line says which one plays and lists the others that might fit, as links. "
            + "Off: only close alternatives are listed, after the emote.");
    }
}
