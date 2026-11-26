using System.Diagnostics;
using System.Text.Json;
using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Naming an emote the character doesn't have offers the closest ones they do have, and plays nothing.</summary>
public class LockedEmoteTests
{
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(PhraseBank.LoadEmbedded()));
    private static readonly List<EmoteInfo> All = JsonSerializer.Deserialize<List<EmoteInfo>>(
        File.ReadAllText(Path.Combine(RepoRulesTests.RepoRoot(), "EmoteIntent.Core", "Data", "emotes.json")),
        JsonSerializerOptions.Web)!;

    [Theory]
    [InlineData("hug", "/hug", "/embrace")]
    [InlineData("/hug", "/hug", "/embrace")]
    [InlineData("embrace", "/embrace", "/hug")]
    [InlineData("big grin", "/biggrin", "/grin")]
    public void OffersTheClosestUnlockedEmotes(string text, string locked, string closest)
    {
        var unlocked = All.Where(emote => emote.Command != locked).ToList();

        var result = Assert.IsType<IntentResult.Locked>(Resolver.Resolve(text, unlocked, All));

        Assert.Equal(locked, result.Wanted.Command);
        Assert.Contains(result.Closest, emote => emote.Command == closest);
        Assert.InRange(result.Closest.Count, 1, IntentResolver.SuggestCount);
    }

    [Fact]
    public void AnUnlockedEmoteStillPlays() =>
        Assert.Equal("/hug", Assert.IsType<IntentResult.Play>(Resolver.Resolve("hug", All, All)).Emote.Command);

    [Fact]
    public void AFeelingNameWhoseListHoldsTheEmotePlaysTheList()
    {
        var unlocked = All.Where(emote => emote.Command != "/sad").ToList();

        Assert.Equal("/slump", Assert.IsType<IntentResult.Play>(Resolver.Resolve("sad", unlocked, All)).Emote.Command);
    }

    [Fact]
    public void WithoutTheFullListALockedNameIsJustText() =>
        Assert.IsNotType<IntentResult.Locked>(Resolver.Resolve("hug", All.Where(emote => emote.Command != "/hug").ToList()));

    [Fact]
    public void FindsTheClosestQuickly()
    {
        var unlocked = All.Where(emote => emote.Command != "/hug").ToList();
        Resolver.Resolve("hug", unlocked, All);

        var clock = Stopwatch.StartNew();
        Resolver.Resolve("hug", unlocked, All);

        Assert.True(clock.ElapsedMilliseconds < 500, $"Took {clock.ElapsedMilliseconds} ms");
    }
}
