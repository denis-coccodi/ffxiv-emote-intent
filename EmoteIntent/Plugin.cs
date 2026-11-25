using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EmoteIntent.Core;

namespace EmoteIntent;

/// <summary>The plugin's entry point: registers /feel and plays the emote the player asked for.</summary>
public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/feel";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IUnlockState UnlockState { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly EmoteCatalog catalog;
    private readonly IntentResolver resolver;
    private readonly SuggestionLinks suggestions;

    public Plugin()
    {
        catalog = new EmoteCatalog(DataManager, UnlockState);
        resolver = new IntentResolver(new PhraseMatcher(PhraseBank.LoadEmbedded()));
        suggestions = new SuggestionLinks(ChatGui, id => catalog.Unlocked().FirstOrDefault(emote => emote.Id == id), Play);
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Play the emote that fits what you mean, e.g. /feel so happy to see you, or /feel hug.",
        });
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler(CommandName);
        suggestions.Dispose();
    }

    private static void Play(EmoteInfo emote)
    {
        Log.Debug("Playing {Command}", emote.Command);
        ChatCommand.Run(emote.Command);
    }

    private void OnCommand(string command, string args)
    {
        var unlocked = catalog.Unlocked();
        if (string.IsNullOrWhiteSpace(args))
        {
            ChatGui.Print($"You have {unlocked.Count} emotes unlocked. Type {CommandName} and what you want to express.");
            return;
        }

        switch (resolver.Resolve(args, unlocked))
        {
            case IntentResult.Play play:
                Play(play.Emote);
                break;
            case IntentResult.Suggest suggest:
                suggestions.Print(suggest.Emotes);
                break;
            default:
                ChatGui.Print($"No unlocked emote fits \"{args.Trim()}\" yet. The phrase bank covers more emotes with each update.");
                break;
        }
    }
}
