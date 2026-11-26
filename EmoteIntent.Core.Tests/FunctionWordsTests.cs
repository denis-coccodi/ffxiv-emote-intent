using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Filler words ("like", "my", "it") must not pull sentences toward one-word phrases like Thumbs Up's "like".</summary>
public class FunctionWordsTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly PhraseMatcher Matcher = new(Bank);
    private static readonly IntentResolver Resolver = new(Matcher);
    private static readonly List<string> AllCommands = Bank.Commands.Values.SelectMany(commands => commands).Distinct().ToList();
    private static readonly List<EmoteInfo> AllUnlocked = AllCommands
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("like books")]
    [InlineData("i like dancing")]
    [InlineData("i like your outfit")]
    [InlineData("i really like you")]
    public void FillerAloneDoesNotOfferThumbsUp(string text) =>
        Assert.DoesNotContain(Matcher.Rank(text, AllCommands, IntentResolver.SuggestCount), match => match.Command == "/thumbsup");

    [Theory]
    [InlineData("like", "/thumbsup")]
    [InlineData("like books", "/read")]
    [InlineData("like reading", "/read")]
    [InlineData("i like dancing", "/dance")]
    [InlineData("i like your outfit", "/pose")]
    [InlineData("that is so cool", "/amazed")]
    [InlineData("my cat is sleeping", "/doze")]
    [InlineData("we did it guys", "/joy")]
    [InlineData("hand on my heart", "/handtoheart")]
    public void PlaysWhatTheRealWordsMean(string text, string command) =>
        Assert.Equal(command, Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Emote.Command);

    [Theory]
    [InlineData(0.6, 0.15)]
    [InlineData(0.95, 0.05)]
    [InlineData(1.0, 0.05)]
    [InlineData(0.775, 0.10)]
    public void AStrongerMatchNeedsASmallerLead(double score, double lead) =>
        Assert.Equal(lead, IntentResolver.RequiredLead(score), precision: 3);
}
