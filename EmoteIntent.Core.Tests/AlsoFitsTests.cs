using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>When an emote plays, other emotes that fit well are offered next to it, never played.</summary>
public class AlsoFitsTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("book", "/read", "/carrybook")]
    [InlineData("sorry", "/easternbow", "/grovel")]
    [InlineData("kiss", "/puckerup", "/blowkiss")]
    [InlineData("drink", "/toast", "/shakedrink")]
    public void OffersOtherEmotesThatFitWell(string text, string played, string alsoFits)
    {
        var play = Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked));

        Assert.Equal(played, play.Emote.Command);
        Assert.Contains(play.AlsoFits, emote => emote.Command == alsoFits);
        Assert.InRange(play.AlsoFits.Count, 1, IntentResolver.SuggestCount);
    }

    [Theory]
    [InlineData("hungry")]
    [InlineData("brb")]
    [InlineData("bored")]
    public void OffersNothingWhenNothingElseFitsWell(string text) =>
        Assert.Empty(Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).AlsoFits);

    [Fact]
    public void NeverOffersTheFallbacksOfTheSameFeeling() =>
        Assert.DoesNotContain(
            Assert.IsType<IntentResult.Play>(Resolver.Resolve("excited", AllUnlocked)).AlsoFits,
            emote => emote.Command is "/psych" or "/delighted" or "/jumpforjoy1");

    [Fact]
    public void AnEmoteAppearsOnceEvenWhenTwoFilesReachIt()
    {
        var matches = new PhraseMatcher(Bank).Rank("scared", AllUnlocked.Select(emote => emote.Command), 10);
        Assert.Equal(matches.Count, matches.Select(match => match.Command).Distinct().Count());
    }
}
