using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>
/// Slang, British and American variants and idioms that once found nothing or the wrong emote, with every emote
/// unlocked. Found by trying local AI models (2026-11-26): they guessed these worse than phrases do.
/// </summary>
public class SlangAndIdiomsTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("stoked", "/jumpforjoy3")]
    [InlineData("so hyper", "/jumpforjoy3")]
    [InlineData("chuffed", "/happy")]
    [InlineData("blissful", "/happy")]
    [InlineData("on cloud nine", "/happy")]
    [InlineData("famished", "/eatriceball")]
    [InlineData("peckish", "/eatriceball")]
    [InlineData("ravenous", "/eatriceball")]
    [InlineData("parched", "/tea")]
    [InlineData("snuggle", "/hug")]
    [InlineData("gutted", "/slump")]
    [InlineData("mopey", "/slump")]
    [InlineData("down in the dumps", "/slump")]
    [InlineData("feeling blue", "/slump")]
    [InlineData("knackered", "/doze")]
    [InlineData("befuddled", "/huh")]
    [InlineData("clueless", "/shrug")]
    [InlineData("indifferent", "/shrug")]
    [InlineData("mortified", "/blush")]
    [InlineData("toodles", "/goodbye")]
    [InlineData("besotted", "/dote")]
    [InlineData("lovesick", "/dote")]
    [InlineData("frazzled", "/headache")]
    [InlineData("pissed off", "/fume")]
    [InlineData("seeing red", "/angry")]
    public void PlaysTheEmoteItMeans(string text, string command) =>
        Assert.Equal(command, Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Emote.Command);
}
