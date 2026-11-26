using System.Text.Json;
using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Data/bundles.txt and how "/feel all eat" finds a bundle.</summary>
public class EmoteBundlesTests
{
    private static readonly EmoteBundles Bundles = EmoteBundles.LoadEmbedded();
    private static readonly List<EmoteInfo> All = JsonSerializer.Deserialize<List<EmoteInfo>>(
        File.ReadAllText(Path.Combine(RepoRulesTests.RepoRoot(), "EmoteIntent.Core", "Data", "emotes.json")),
        JsonSerializerOptions.Web)!;

    [Fact]
    public void EveryBundleNamesRealEmotesOnce()
    {
        var commands = All.Select(emote => emote.Command).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var bundle in Bundles.All)
        {
            Assert.True(bundle.Commands.Count >= 2, $"{bundle.Name} has fewer than 2 emotes");
            Assert.All(bundle.Commands, command => Assert.True(commands.Contains(command), $"{bundle.Name}: no emote {command}"));
            Assert.Equal(bundle.Commands.Count, bundle.Commands.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }

    [Fact]
    public void NoNameBelongsToTwoBundles()
    {
        var names = Bundles.All.SelectMany(bundle => bundle.Names).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AskingForABundleNeverHidesAPhrase()
    {
        var matcher = new PhraseMatcher(PhraseBank.LoadEmbedded());
        var hidden = Bundles.All.SelectMany(bundle => bundle.Names).Prepend(string.Empty)
            .Select(name => $"all {name}".Trim())
            .Where(matcher.IsPhrase)
            .ToList();
        Assert.Empty(hidden);
    }

    [Theory]
    [InlineData("/eat", "Eat")]
    [InlineData("/cheeron", "Penlight")]
    [InlineData("/cheerwave", "Penlight")]
    [InlineData("/cheerrhythm", "Penlight")]
    [InlineData("/cheerlight", "Penlight")]
    [InlineData("/jumpforjoy", "Jump")]
    [InlineData("/rangerpose", "Pose")]
    [InlineData("/paint", "Paint")]
    public void EveryEmoteOfAKindIsInItsBundle(string prefix, string name)
    {
        var bundle = Bundles.All.Single(bundle => bundle.Name == name);
        Assert.All(All.Where(emote => emote.Command.StartsWith(prefix, StringComparison.Ordinal)),
            emote => Assert.Contains(emote.Command, bundle.Commands));
    }

    [Fact]
    public void EveryExpressionIsInTheExpressionBundle()
    {
        var bundle = Bundles.All.Single(bundle => bundle.Name == "Expression");
        Assert.All(All.Where(emote => emote.Category == "Expressions"), emote => Assert.Contains(emote.Command, bundle.Commands));
    }

    [Theory]
    [InlineData("all eat", "Eat")]
    [InlineData("All the jumps!", "Jump")]
    [InlineData("every dance", "Dance")]
    [InlineData("all of the penlights", "Penlight")]
    [InlineData("all food", "Eat")]
    [InlineData("all expressions", "Expression")]
    public void FindsTheBundleAskedFor(string text, string name) =>
        Assert.Equal(name, Bundles.Request(text)?.Bundle?.Name);

    [Theory]
    [InlineData("all")]
    [InlineData("every")]
    [InlineData("All!")]
    public void AllAloneAsksForTheListOfBundles(string text)
    {
        var request = Bundles.Request(text);
        Assert.NotNull(request);
        Assert.Null(request.Bundle);
    }

    [Theory]
    [InlineData("all good")]
    [InlineData("all right")]
    [InlineData("eat")]
    [InlineData("jumps")]
    [InlineData("hug all")]
    public void OtherTextIsNotABundleRequest(string text) =>
        Assert.Null(Bundles.Request(text));

    [Fact]
    public void KnowsTheBundleOfAnEmote()
    {
        Assert.Equal("Eat", Bundles.Of("/eatpizza")?.Name);
        Assert.Null(Bundles.Of("/hug"));
    }

    [Fact]
    public void ListsUnlockedEmotesInTheBundlesOrderOnceEach()
    {
        var salute = Bundles.All.Single(bundle => bundle.Name == "Salute");
        var unlocked = All.Where(emote => emote.Command is "/gcsalute" or "/salute").Reverse().ToList();

        var listed = EmoteBundles.UnlockedOf(salute, unlocked).Select(emote => emote.Command);

        Assert.Equal(["/salute", "/gcsalute"], listed);
    }
}
