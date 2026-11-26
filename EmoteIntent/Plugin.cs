using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EmoteIntent.Core;

namespace EmoteIntent;

/// <summary>The plugin's entry point: registers /feel and plays the emote the player asked for.</summary>
public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/feel";
    private const string ConfigCommandName = "/feelconfig";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IUnlockState UnlockState { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly Configuration configuration;
    private readonly EmoteCatalog catalog;
    private readonly PhraseMatcher matcher;
    private readonly IntentResolver resolver;
    private readonly SuggestionLinks suggestions;
    private readonly WindowSystem windows = new("EmoteIntent");
    private readonly SettingsWindow settings;

    public Plugin()
    {
        configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (configuration.Migrate())
        {
            PluginInterface.SavePluginConfig(configuration);
        }
        catalog = new EmoteCatalog(DataManager, UnlockState);
        matcher = new PhraseMatcher(PhraseBank.LoadEmbedded());
        resolver = new IntentResolver(matcher);
        suggestions = new SuggestionLinks(ChatGui, id => catalog.Unlocked().FirstOrDefault(emote => emote.Id == id), Play);

        settings = new SettingsWindow(configuration, () => PluginInterface.SavePluginConfig(configuration));
        windows.AddWindow(settings);
        PluginInterface.UiBuilder.Draw += windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += settings.Toggle;

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Play the emote that fits what you mean, e.g. /feel so happy to see you, or /feel hug. "
                + "End with \"motion\" to play it without the chat line, or \"text\" to show it.",
        });
        CommandManager.AddHandler(ConfigCommandName, new CommandInfo((_, _) => settings.Toggle())
        {
            HelpMessage = "Open the Emote Intent settings.",
        });
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler(ConfigCommandName);
        CommandManager.RemoveHandler(CommandName);
        PluginInterface.UiBuilder.OpenConfigUi -= settings.Toggle;
        PluginInterface.UiBuilder.Draw -= windows.Draw;
        windows.RemoveAllWindows();
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
        switch (resolver.Resolve(request.Text, unlocked, catalog.All()))
        {
            case IntentResult.Play play when configuration.WhenFound == WhenFound.AlwaysPick:
                suggestions.PrintPick([play.Emote, .. play.Alternatives], request);
                break;
            case IntentResult.Play play when configuration.WhenFound == WhenFound.AskWhenSeveral
                && !play.Named && play.Alternatives.Count > 0:
                suggestions.Print([play.Emote, .. play.Alternatives], request);
                break;
            case IntentResult.Play play:
                Play(play.Emote, request);
                if (configuration.ShowPlayedEmote)
                {
                    suggestions.PrintPlayed(play.Emote, request);
                }

                if (configuration.WhenFound == WhenFound.PlayBest && play.Alternatives.Count > 0)
                {
                    suggestions.PrintAlsoFits(play.Alternatives, request);
                }

                break;
            case IntentResult.Locked locked:
                suggestions.PrintLocked(locked.Wanted, locked.Closest, request);
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
