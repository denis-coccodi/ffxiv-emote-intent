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
    private readonly PhraseMatcher matcher;
    private readonly IntentResolver resolver;
    private readonly SuggestionLinks suggestions;

    public Plugin()
    {
        catalog = new EmoteCatalog(DataManager, UnlockState);
        matcher = new PhraseMatcher(PhraseBank.LoadEmbedded());
        resolver = new IntentResolver(matcher);
        suggestions = new SuggestionLinks(ChatGui, id => catalog.Unlocked().FirstOrDefault(emote => emote.Id == id), Play);
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Play the emote that fits what you mean, e.g. /feel so happy to see you, or /feel hug. "
                + "End with \"motion\" to play it without the chat line, or \"text\" to show it.",
        });
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler(CommandName);
        suggestions.Dispose();
    }

    private static void Play(EmoteInfo emote, FeelRequest request)
    {
        var command = request.CommandFor(emote);
        Log.Debug("Playing {Command}", command);
        ChatCommand.Run(command);
    }

    private void OnCommand(string command, string args)
    {
        var unlocked = catalog.Unlocked();
        if (string.IsNullOrWhiteSpace(args))
        {
            ChatGui.Print($"You have {unlocked.Count} emotes unlocked. Type {CommandName} and what you want to express.");
            return;
        }

        var request = FeelRequest.Parse(args, matcher.IsPhrase);
        switch (resolver.Resolve(request.Text, unlocked))
        {
            case IntentResult.Play play:
                Play(play.Emote, request);
                break;
            case IntentResult.Suggest suggest:
                suggestions.Print(suggest.Emotes, request);
                break;
            default:
                ChatGui.Print($"No unlocked emote fits \"{request.Text}\" yet. The phrase bank covers more emotes with each update.");
                break;
        }
    }
}
