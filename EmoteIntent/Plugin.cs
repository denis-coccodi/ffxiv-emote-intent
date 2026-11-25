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

    public Plugin()
    {
        catalog = new EmoteCatalog(DataManager, UnlockState);
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Play the emote that fits what you mean, e.g. /feel hug (matching by meaning is coming).",
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

        var emote = EmoteNameMatcher.Match(args, unlocked);
        if (emote is null)
        {
            ChatGui.Print($"No unlocked emote is called \"{args.Trim()}\". Matching by meaning isn't built yet.");
            return;
        }

        Log.Debug("Playing {Command} for \"{Args}\"", emote.Command, args);
        ChatCommand.Run(emote.Command);
    }
}
