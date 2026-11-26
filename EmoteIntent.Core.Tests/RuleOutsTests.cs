using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Words that rule emotes out ("cold" never means Tremble), unless other words keep them ("cold and scared").</summary>
public class RuleOutsTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Fact]
    public void EveryRuledOutEmoteIsInThePhraseBank() =>
        Assert.All(RuleOuts.LoadEmbedded().Commands, command => Assert.Contains(command, AllUnlocked.Select(emote => emote.Command)));

    [Theory]
    [InlineData("shaking from the cold", true)]
    [InlineData("so many chills", true)]
    [InlineData("cold and scared", false)]
    [InlineData("frozen in fear", false)]
    [InlineData("shaking", false)]
    public void ColdRulesOutTrembleUnlessFearIsSaid(string text, bool ruledOut) =>
        Assert.Equal(ruledOut, RuleOuts.LoadEmbedded().RuledOut(text).Contains("/tremble"));

    [Fact]
    public void ALineThatIsNotARuleIsRefused() =>
        Assert.Throws<InvalidDataException>(() => RuleOuts.Parse("cold /tremble"));

    [Fact]
    public void ColdAndScaredStillOffersTremble()
    {
        var result = Resolver.Resolve("cold and scared", AllUnlocked);
        var offered = result switch
        {
            IntentResult.Play play => [play.Emote, .. play.Alternatives],
            IntentResult.Suggest suggest => suggest.Emotes,
            _ => [],
        };
        Assert.Contains(offered, emote => emote.Command == "/tremble");
    }

    [Fact]
    public void NamingARuledOutEmoteStillPlaysIt() =>
        Assert.Equal("/tremble", Assert.IsType<IntentResult.Play>(Resolver.Resolve("tremble", AllUnlocked)).Emote.Command);
}
