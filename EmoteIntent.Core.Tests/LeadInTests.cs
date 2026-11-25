using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Lead-in words ("feel like", "i'm feeling", "i want to be") say nothing about the emote.</summary>
public class LeadInTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("feel like being spooky", "spooky")]
    [InlineData("I'm feeling so happy", "happy")]
    [InlineData("i want to be spooky", "spooky")]
    [InlineData("i feel like dancing", "dancing")]
    [InlineData("just chilling", "chilling")]
    [InlineData("so", "so")]
    [InlineData("hug", "hug")]
    public void StripsTheLeadInWords(string text, string rest) => Assert.Equal(rest, LeadIn.Strip(text));

    [Theory]
    [InlineData("feel like being spooky", "/frighten")]
    [InlineData("i'm feeling spooky", "/frighten")]
    [InlineData("i want to be spooky", "/frighten")]
    [InlineData("spooky", "/frighten")]
    [InlineData("feeling happy", "/happy")]
    [InlineData("i feel like dancing", "/dance")]
    [InlineData("i'm really tired", "/doze")]
    [InlineData("feel like crying", "/cry")]
    [InlineData("i feel so proud", "/beam")]
    [InlineData("feeling jealous", "/sulk")]
    [InlineData("i feel sick", "/headache")]
    [InlineData("just chilling", "/lean")]
    [InlineData("i wanna hug you", "/hug")]
    [InlineData("so happy to see you", "/greet")]
    [InlineData("be quiet", "/shush")]
    public void PlaysWhatFollowsTheLeadIn(string text, string command) =>
        Assert.Equal(command, Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Emote.Command);
}
