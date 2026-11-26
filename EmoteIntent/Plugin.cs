using Dalamud.Game.ClientState.Conditions;
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
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly Configuration configuration;
    private readonly EmoteCatalog catalog;
    private readonly PhraseMatcher matcher;
    private readonly IntentResolver resolver;
    private readonly EmoteBundles bundles = EmoteBundles.LoadEmbedded();
    private readonly SuggestionLinks suggestions;
    private readonly WindowSystem windows = new("EmoteIntent");
    private readonly SettingsWindow settings;
    private readonly MainWindow main;

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
        suggestions = new SuggestionLinks(ChatGui, catalog.Unlocked, bundles, Play);

        settings = new SettingsWindow(configuration, () => PluginInterface.SavePluginConfig(configuration));
        windows.AddWindow(settings);
        main = new MainWindow(() => catalog.Unlocked().Count, () => settings.IsOpen = true);
        windows.AddWindow(main);
        PluginInterface.UiBuilder.Draw += windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += settings.Toggle;
        PluginInterface.UiBuilder.OpenMainUi += main.Toggle;

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Play the emote that fits what you mean: /feel so happy to see you, /feel hug, /feel all dances. "
                + "/feel alone (or /feel ?) opens the help window.",
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
        PluginInterface.UiBuilder.OpenMainUi -= main.Toggle;
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
        if (Guide.IsAsked(args))
        {
            main.IsOpen = true;

            return;
        }

        var request = FeelRequest.Parse(args, matcher.IsPhrase);
        // "all eat" lists a whole bundle, "all" the bundles; typos fixed as for any text ("all jumsp").
        if ((bundles.Request(request.Text) ?? bundles.Request(matcher.Correct(request.Text).Text)) is { } asked)
        {
            if (asked.Bundle is null)
            {
                suggestions.PrintBundles(request);
            }
            else
            {
                suggestions.PrintBundle(asked.Bundle, request);
            }

            return;
        }

        // Water emotes only play in water: elsewhere they're left out, unless the player names one (the game then says why).
        var here = EmoteNameMatcher.Match(request.Text, unlocked) is not null
            ? unlocked
            : WaterEmotes.Playable(unlocked, Condition[ConditionFlag.Swimming], Condition[ConditionFlag.Diving]);
        var result = resolver.Resolve(request.Text, here, catalog.All());
        if (result.Corrections.Count > 0)
        {
            // Typos are guessed: saying how they were read shows a wrong guess at once.
            ChatGui.Print("Read " + string.Join(", ", result.Corrections.Select(fix => $"\"{fix.Typed}\" as \"{fix.Read}\""))
                + ".");
        }

        switch (result)
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
                    suggestions.PrintAlsoFits(play.Emote, play.Alternatives, request);
                }

                break;
            case IntentResult.Locked locked:
                suggestions.PrintLocked(locked.Wanted, locked.Closest, request);
                break;
            case IntentResult.Suggest suggest:
                suggestions.Print(suggest.Emotes, request);
                break;
            default:
                ChatGui.Print($"No unlocked emote fits \"{request.Text}\" yet. /feel ? shows how to use /feel.");
                break;
        }
    }
}
