using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>
/// Rules from the user's blind judging of the AI evaluation (2026-11-26), written as everyday texts other than the
/// judged sentences (those stay unseen in tools/AiEval/cases.txt): big joy is energetic, a head shake for "nice try",
/// Linkpearl for "call me", Sulk for missing old friends.
/// </summary>
public class JudgingNotesTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("on fire", "/jumpforjoy3")]
    [InlineData("killing it", "/jumpforjoy3")]
    [InlineData("so so happy", "/joy")]
    [InlineData("over the moon", "/joy")]
    [InlineData("i won", "/victorypose")]
    [InlineData("nice try", "/no")]
    [InlineData("not buying it", "/no")]
    [InlineData("you can't fool me", "/no")]
    [InlineData("give me a call", "/linkpearl")]
    [InlineData("dm me", "/linkpearl")]
    [InlineData("i miss my friends", "/sulk")]
    [InlineData("miss the good old days", "/sulk")]
    public void PlaysTheEmoteItMeans(string text, string command) =>
        Assert.Equal(command, Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, AllUnlocked)).Emote.Command);
}
