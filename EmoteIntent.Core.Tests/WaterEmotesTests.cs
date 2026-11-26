using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Splash, Water Float and Water Flip only play in water, so they aren't chosen on land.</summary>
public class WaterEmotesTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    private static readonly string[] WaterAndHug = ["/hug", "/splash", "/waterfloat", "/waterflip"];

    [Theory]
    [InlineData(false, false, "/hug")]
    [InlineData(true, false, "/hug /splash /waterfloat")]
    [InlineData(false, true, "/hug /splash /waterfloat /waterflip")]
    public void WaterEmotesNeedWater(bool swimming, bool diving, string expected)
    {
        var unlocked = WaterAndHug.Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
            .ToList();
        Assert.Equal(expected, string.Join(' ', WaterEmotes.Playable(unlocked, swimming, diving).Select(emote => emote.Command)));
    }

    [Theory]
    [InlineData("floating")]
    [InlineData("splash")]
    public void OnLandAWaterWordDoesNotPlayAWaterEmote(string text)
    {
        var result = Resolver.Resolve(text, WaterEmotes.Playable(AllUnlocked, swimming: false, diving: false));
        Assert.False(result is IntentResult.Play { Emote.Command: "/splash" or "/waterfloat" or "/waterflip" });
    }

    [Theory]
    [InlineData("smells bad")]
    [InlineData("it stinks in here")]
    [InlineData("who farted")]
    public void ABadSmellPlaysDazed(string text) =>
        Assert.Equal("/dazed", Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Emote.Command);
}
