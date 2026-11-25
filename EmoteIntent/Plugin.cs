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

    public Plugin()
    {
        catalog = new EmoteCatalog(DataManager, UnlockState);
        resolver = new IntentResolver(new PhraseMatcher(PhraseBank.LoadEmbedded()));
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Play the emote that fits what you mean, e.g. /feel so happy to see you, or /feel hug.",
        });
    }

    public void Dispose() => CommandManager.RemoveHandler(CommandName);

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
                Log.Debug("Playing {Command} for \"{Args}\"", play.Emote.Command, args);
                ChatCommand.Run(play.Emote.Command);
                break;
            case IntentResult.Suggest suggest:
                var options = string.Join(", ", suggest.Emotes.Select(emote => $"{emote.Command} ({emote.Name})"));
                ChatGui.Print($"Not sure which you mean: {options}. Type the one you want.");
                break;
            default:
                ChatGui.Print($"No unlocked emote fits \"{args.Trim()}\" yet. The phrase bank covers more emotes with each update.");
                break;
        }
    }
}
