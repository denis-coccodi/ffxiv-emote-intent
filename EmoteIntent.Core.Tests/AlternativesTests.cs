using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>What the "always show" setting lists before an emote plays: every other emote worth suggesting.</summary>
public class AlternativesTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Fact]
    public void ListsMoreThanAlsoFitsAndNeverThePlayedEmote()
    {
        var play = Assert.IsType<IntentResult.Play>(Resolver.Resolve("book", AllUnlocked));

        Assert.Superset(play.AlsoFits.ToHashSet(), play.Alternatives.ToHashSet());
        Assert.DoesNotContain(play.Alternatives, emote => emote.Command == play.Emote.Command);
        Assert.InRange(play.Alternatives.Count, play.AlsoFits.Count, IntentResolver.SuggestCount);
    }

    [Fact]
    public void AnEmoteNamedOutrightAlsoGetsAlternatives()
    {
        var play = Assert.IsType<IntentResult.Play>(Resolver.Resolve("hug", AllUnlocked));

        Assert.Equal("/hug", play.Emote.Command);
        Assert.True(play.Named);
        Assert.NotEmpty(play.Alternatives);
        Assert.DoesNotContain(play.Alternatives, emote => emote.Command == "/hug");
    }

    [Theory]
    [InlineData("give me a big hug")]
    [InlineData("book")]
    public void APhraseMatchIsNotANamedEmote(string text) =>
        Assert.False(Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Named);
}
