using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace EmoteIntent;

/// <summary>
/// The plugin's main window, opened from the plugin list's Open button. One tab for now, Help: how to use /feel
/// (also opened by "/feel" alone, "/feel help" or "/feel ?").
/// </summary>
internal sealed class MainWindow : Window
{
    private readonly Func<int> unlockedCount;
    private readonly Action openSettings;
    private int unlocked;

    public MainWindow(Func<int> unlockedCount, Action openSettings)
        : base("Emote Intent###EmoteIntentMain")
    {
        this.unlockedCount = unlockedCount;
        this.openSettings = openSettings;
        Size = new Vector2(520, 280);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    // Counted once per opening: checking every emote's unlock each frame would be wasted work.
    public override void OnOpen() => unlocked = unlockedCount();

    public override void Draw()
    {
        using var tabs = ImRaii.TabBar("##EmoteIntentTabs");
        if (!tabs)
        {
            return;
        }

        using var help = ImRaii.TabItem("Help");
        if (help)
        {
            DrawHelp();
        }
    }

    private void DrawHelp()
    {
        ImGui.TextWrapped($"Type /feel and what you mean, and the emote that fits plays. You have {unlocked} emotes unlocked.");
        ImGui.Spacing();
        foreach (var line in Guide.Lines)
        {
            ImGui.Bullet();
            ImGui.SameLine();
            ImGui.TextWrapped(line);
        }

        ImGui.Spacing();
        if (ImGui.Button("Settings"))
        {
            openSettings();
        }

        ImGui.SameLine();
        ImGui.TextDisabled("(or /feelconfig)");
    }
}
