using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using EmoteIntent.Core.Ai;

namespace EmoteIntent;

/// <summary>The settings window: opened from the plugin list's gear button or with /feelconfig.</summary>
internal sealed class SettingsWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action save;
    private readonly Func<Task<OllamaStatus>> checkAi;
    private string aiStatus = "";

    public SettingsWindow(Configuration configuration, Action save, Func<Task<OllamaStatus>> checkAi)
        : base("Emote Intent settings###EmoteIntentSettings")
    {
        this.configuration = configuration;
        this.save = save;
        this.checkAi = checkAi;
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

        ImGui.Spacing();
        ImGui.Separator();
        DrawAi();
    }

    // The opt-in AI: a model in Ollama on this PC, asked only when the phrase bank isn't sure.
    private void DrawAi()
    {
        var useAi = configuration.UseAi;
        if (ImGui.Checkbox("When /feel isn't sure, ask an AI model on this PC", ref useAi))
        {
            Set(() => configuration.UseAi = useAi);
        }

        ImGui.TextWrapped("Free and private: the model runs in Ollama on your own PC, and what you type never leaves it. "
            + "Install Ollama (ollama.com), then download the model once: ollama pull gemma3:4b (about 3 GB). "
            + "The first answer after a while takes a few seconds while the model loads.");

        using (ImRaii.Disabled(!configuration.UseAi))
        {
            var model = configuration.AiModel;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("Model", ref model, 64) && model.Trim().Length > 0)
            {
                Set(() => configuration.AiModel = model.Trim());
            }

            ImGui.SameLine();
            if (ImGui.Button("Check"))
            {
                aiStatus = "Checking…";
                _ = Check();
            }

            if (aiStatus.Length > 0)
            {
                ImGui.TextWrapped(aiStatus);
            }
        }
    }

    private async Task Check()
    {
        var model = configuration.AiModel;
        aiStatus = await checkAi().ConfigureAwait(false) switch
        {
            OllamaStatus.Ready => $"Ready: Ollama is running with {model}.",
            OllamaStatus.ModelMissing => $"Ollama is running, but {model} isn't downloaded: run ollama pull {model}",
            _ => "Ollama isn't running on this PC (127.0.0.1:11434). Start it, or install it from ollama.com.",
        };
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
