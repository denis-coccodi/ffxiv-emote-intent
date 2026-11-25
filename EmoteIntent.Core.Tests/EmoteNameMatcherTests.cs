using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

public class EmoteNameMatcherTests
{
    private static readonly EmoteInfo Hug = new(1, "Hug", "/hug", [], "General");
    private static readonly EmoteInfo BigClap = new(2, "Big Clap", "/bigclap", ["/bclap"], "Special");
    private static readonly EmoteInfo[] Emotes = [Hug, BigClap];

    [Theory]
    [InlineData("hug")]
    [InlineData("Hug")]
    [InlineData("/hug")]
    [InlineData("  HUG ")]
    public void MatchesTheNameOrCommandIgnoringCaseAndSlash(string text) =>
        Assert.Same(Hug, EmoteNameMatcher.Match(text, Emotes));

    [Theory]
    [InlineData("big clap")]
    [InlineData("bigclap")]
    [InlineData("/bclap")]
    public void MatchesNamesWithSpacesAndAliases(string text) =>
        Assert.Same(BigClap, EmoteNameMatcher.Match(text, Emotes));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    [InlineData("so happy to see you")]
    [InlineData("hu")]
    public void ReturnsNullWhenNothingIsNamedExactly(string text) =>
        Assert.Null(EmoteNameMatcher.Match(text, Emotes));
}
