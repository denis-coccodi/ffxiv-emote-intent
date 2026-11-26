using System.Text.Json;
using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Flying, hovering and jumping play the most fitting emote the character owns, also without the rare ones.</summary>
public class MovementFeelingsTests
{
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(PhraseBank.LoadEmbedded()));
    private static readonly List<EmoteInfo> All = JsonSerializer.Deserialize<List<EmoteInfo>>(
        File.ReadAllText(Path.Combine(RepoRulesTests.RepoRoot(), "EmoteIntent.Core", "Data", "emotes.json")),
        JsonSerializerOptions.Web)!;

    // A character without the store and figure emotes: no Advent of Light, Simulation or Jump for Joy.
    private static readonly List<EmoteInfo> WithoutRareOnes = All
        .Where(emote => emote.Command is not ("/adventoflight" or "/simulationm" or "/simulationf")
            && !emote.Command.StartsWith("/jumpforjoy", StringComparison.Ordinal))
        .ToList();

    [Theory]
    [InlineData("flying", "/adventoflight")]
    [InlineData("hovering", "/simulationm")]
    [InlineData("levitating", "/simulationm")]
    [InlineData("jumping", "/jumpforjoy3")]
    public void PlaysTheBestFitWithEverything(string text, string command) =>
        Assert.Equal(command, Played(text, All));

    [Theory]
    [InlineData("flying", "/blownaway")]
    [InlineData("i'm flying", "/blownaway")]
    [InlineData("jumping", "/cheerjumpred")]
    [InlineData("jump", "/cheerjumpred")]
    public void FallsBackToAnEmoteTheCharacterOwns(string text, string command) =>
        Assert.Equal(command, Played(text, WithoutRareOnes));

    [Fact]
    public void HoveringWithoutAnyFloatingEmoteDoesNotOfferStepDance() =>
        Assert.DoesNotContain(Offered(Resolver.Resolve("hovering", WithoutRareOnes)), emote => emote.Command == "/stepdance");

    private static string Played(string text, List<EmoteInfo> unlocked) =>
        Assert.IsType<IntentResult.Play>(Resolver.Resolve(text, unlocked)).Emote.Command;

    private static IEnumerable<EmoteInfo> Offered(IntentResult result) => result switch
    {
        IntentResult.Play play => [play.Emote, .. play.Alternatives],
        IntentResult.Suggest suggest => suggest.Emotes,
        _ => [],
    };
}
