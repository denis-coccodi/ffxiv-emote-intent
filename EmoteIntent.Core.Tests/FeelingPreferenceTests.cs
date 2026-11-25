using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Strong feelings prefer a full-body emote and fall back, in order, to what the character has.</summary>
public class FeelingPreferenceTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));

    [Theory]
    [InlineData("/jumpforjoy3", "/jumpforjoy3", "/psych", "/delighted")]
    [InlineData("/jumpforjoy1", "/jumpforjoy1", "/psych", "/delighted")]
    [InlineData("/psych", "/psych", "/delighted")]
    [InlineData("/delighted", "/delighted")]
    public void ExcitedPlaysTheMostExpressiveEmoteTheCharacterHas(string expected, params string[] unlocked) =>
        Assert.Equal(expected, Played("excited", unlocked));

    [Theory]
    [InlineData("sad", "/slump", "/slump", "/sad")]
    [InlineData("sad", "/sad", "/sad")]
    [InlineData("scared", "/tremble", "/tremble", "/scared")]
    [InlineData("amazed", "/wow", "/wow", "/amazed")]
    [InlineData("annoyed", "/fume", "/fume", "/annoyed")]
    public void AFeelingThatIsAlsoAnEmoteNamePrefersTheFullBodyEmote(string text, string expected, params string[] unlocked) =>
        Assert.Equal(expected, Played(text, unlocked));

    [Fact]
    public void AnEmoteNamedOutsideAnyFeelingStillPlaysItself() => Assert.Equal("/hug", Played("hug", "/hug", "/embrace"));

    private static string Played(string text, params string[] commands)
    {
        var unlocked = commands.Select((command, index) => new EmoteInfo(
            (uint)index + 1, Name(command), command, [], "General")).ToList();
        return Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, unlocked)).Emote.Command;
    }

    // The emote's name as the game shows it, enough for the name matcher ("/sad" → "Sad").
    private static string Name(string command) => char.ToUpperInvariant(command[1]) + command[2..];
}
