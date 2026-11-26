using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>
/// Feelings, states and roleplay words that once found nothing or the wrong emote (from a 1,000-word sweep,
/// 2026-11-26), with every emote unlocked.
/// </summary>
public class FeelingsAndStatesTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("trapped", "/panic")]
    [InlineData("cornered", "/panic")]
    [InlineData("defensive", "/guard")]
    [InlineData("tank", "/guard")]
    [InlineData("block", "/guard")]
    [InlineData("paranoid", "/worried")]
    [InlineData("remorseful", "/slump")]
    [InlineData("apologetic", "/easternbow")]
    [InlineData("injured", "/ouch")]
    [InlineData("intrigued", "/consider")]
    [InlineData("vengeful", "/malevolence")]
    [InlineData("aggressive", "/battlestance")]
    [InlineData("sneaking", "/scheme")]
    [InlineData("pensive", "/think")]
    [InlineData("compassionate", "/comfort")]
    [InlineData("nurse", "/comfort")]
    [InlineData("polite", "/bow")]
    [InlineData("offended", "/vexed")]
    [InlineData("i surrender", "/kneel")]
    [InlineData("party wipe", "/playdead")]
    [InlineData("spying", "/lookout")]
    [InlineData("partying", "/dance")]
    [InlineData("wp", "/clap")]
    [InlineData(@"\o/", "/hurray")]
    [InlineData("fond", "/dote")]
    [InlineData("irate", "/furious")]
    [InlineData("delirious", "/dazed")]
    [InlineData("intimidated", "/tremble")]
    [InlineData("brave", "/determined")]
    [InlineData("chatty", "/converse")]
    [InlineData("clumsy", "/stagger")]
    [InlineData("euphoric", "/joy")]
    [InlineData("frantic", "/panic")]
    [InlineData("snarky", "/airquotes")]
    [InlineData("stubborn", "/insist")]
    [InlineData("brokenhearted", "/cry")]
    [InlineData("smells delicious", "/amazed")]
    [InlineData("that smells so good", "/amazed")]
    [InlineData("mouth watering", "/amazed")]
    public void PlaysTheEmoteItMeans(string text, string command) =>
        Assert.Equal(command, Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Emote.Command);

    // Tremble is fear only (user, 2026-11-26): being cold never plays or offers it, even without Shiver.
    [Theory]
    [InlineData("i'm cold")]
    [InlineData("so cold")]
    [InlineData("freezing")]
    [InlineData("brrr")]
    [InlineData("shivering")]
    [InlineData("chills")]
    [InlineData("teeth chattering")]
    public void ColdNeverPlaysTremble(string text)
    {
        Assert.Equal("/shiver", Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Emote.Command);
        var withoutShiver = AllUnlocked.Where(emote => emote.Command != "/shiver").ToList();
        var offered = Resolver.Resolve(text, withoutShiver) switch
        {
            IntentResult.Play play => [play.Emote, .. play.Alternatives],
            IntentResult.Suggest suggest => suggest.Emotes,
            _ => [],
        };
        Assert.DoesNotContain(offered, emote => emote.Command == "/tremble");
    }

    // "shaking" is shared with "shaking with fear": without Shiver, Tremble may be suggested but never plays.
    [Fact]
    public void ShakingFromTheColdNeverPlaysTremble()
    {
        Assert.Equal("/shiver", Assert.IsType<IntentResult.Play>(Resolver.Resolve("shaking from the cold", AllUnlocked)).Emote.Command);
        var withoutShiver = AllUnlocked.Where(emote => emote.Command != "/shiver").ToList();
        Assert.False(Resolver.Resolve("shaking from the cold", withoutShiver) is IntentResult.Play { Emote.Command: "/tremble" });
    }
}
