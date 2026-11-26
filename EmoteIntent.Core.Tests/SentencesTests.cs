using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>
/// Whole sentences from raids, chat and roleplay (a 247-sentence round, 2026-11-26), with every emote unlocked. The
/// phrase bank only knows the situations its phrases cover: tools/AiEval measures how it does on sentences it wasn't
/// written for.
/// </summary>
public class SentencesTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("we wiped at 1 percent", "/clutchhead")]
    [InlineData("someone stood in the fire again", "/facepalm")]
    [InlineData("who pulled the boss early", "/furious")]
    [InlineData("the loot gods hate me", "/slump")]
    [InlineData("praying to rngesus", "/pray")]
    [InlineData("i topped the parses", "/flex")]
    [InlineData("i'll take the aggro", "/guard")]
    [InlineData("duty pop finally", "/joy")]
    [InlineData("we got a sprout in the party", "/welcome")]
    [InlineData("the market board is a scam", "/fume")]
    [InlineData("i'm heading to bed", "/goodbye")]
    [InlineData("i'm crying laughing", "/laugh")]
    [InlineData("that's not funny", "/straightface")]
    [InlineData("it wasn't me", "/shrug")]
    [InlineData("excuse me", "/bow")]
    [InlineData("talk to the hand", "/deny")]
    [InlineData("deep breaths", "/breathcontrol")]
    [InlineData("there's a spider", "/panic")]
    [InlineData("this makes my blood boil", "/furious")]
    [InlineData("stares into the distance", "/reflect")]
    public void PlaysTheEmoteItMeans(string text, string command) =>
        Assert.Equal(command, Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Emote.Command);
}
