using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Typos are read as the phrase bank's words; words it knows, short words and unrelated words stay as typed.</summary>
public class SpellCorrectorTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly PhraseMatcher Matcher = new(Bank);
    private static readonly IntentResolver Resolver = new(Matcher);
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("exicted", "excited")]
    [InlineData("so exicted!", "so excited!")]
    [InlineData("hapy", "happy")]
    [InlineData("happpy", "happy")]
    [InlineData("helo", "hello")]
    [InlineData("hugg", "hug")]
    [InlineData("spooookyy", "spooky")]
    [InlineData("tierd", "tired")]
    [InlineData("angyr", "angry")]
    [InlineData("suprised", "surprised")]
    [InlineData("embarassed", "embarrassed")]
    [InlineData("confussed", "confused")]
    [InlineData("greatful", "grateful")]
    [InlineData("thnak you", "thank you")]
    [InlineData("laughign", "laughing")]
    [InlineData("scraed", "scared")]
    [InlineData("lonley", "lonely")]
    [InlineData("jelous", "jealous")]
    [InlineData("danse", "dance")]
    public void ReadsTyposAsTheBanksWords(string typed, string read) =>
        Assert.Equal(read, Matcher.Correct(typed).Text);

    [Theory]
    [InlineData("excited")]
    [InlineData("hugs")]
    [InlineData("hum")]
    [InlineData("pizza")]
    [InlineData("catgirl")]
    [InlineData("kupo")]
    [InlineData("o7")]
    [InlineData("<3")]
    public void LeavesKnownShortAndUnrelatedWordsAlone(string typed) =>
        Assert.Empty(Matcher.Correct(typed).Corrections);

    [Theory]
    [InlineData("exicted", "excited")]
    [InlineData("so hapy to see you", "so happy to see you")]
    [InlineData("tierd", "tired")]
    [InlineData("i'm so lonley", "i'm so lonely")]
    [InlineData("spooookyy", "spooky")]
    [InlineData("thnak you", "thank you")]
    public void ATypoFindsWhatTheRightSpellingFinds(string typed, string spelled)
    {
        var expected = Resolver.Resolve(spelled, AllUnlocked);
        var actual = Resolver.Resolve(typed, AllUnlocked);

        Assert.Equal(Emotes(expected), Emotes(actual));
        Assert.NotEmpty(actual.Corrections);
        Assert.Empty(expected.Corrections);
    }

    [Fact]
    public void AnEmoteNameWithATypoStillPlaysIt() =>
        Assert.Equal("/hug", Assert.IsType<IntentResult.Play>(Resolver.Resolve("hugg", AllUnlocked)).Emote.Command);

    [Theory]
    [InlineData("exicted", "excited", 1)]
    [InlineData("excited", "exicted", 1)]
    [InlineData("helo", "hello", 1)]
    [InlineData("catgirl", "fangirl", 2)]
    [InlineData("abc", "abc", 0)]
    public void CountsTypos(string a, string b, int typos) =>
        Assert.Equal(typos, SpellCorrector.Distance(a, b));

    private static List<string> Emotes(IntentResult result) => result switch
    {
        IntentResult.Play play => [play.Emote.Command, .. play.Alternatives.Select(emote => emote.Command)],
        IntentResult.Suggest suggest => suggest.Emotes.Select(emote => emote.Command).ToList(),
        _ => [],
    };
}
