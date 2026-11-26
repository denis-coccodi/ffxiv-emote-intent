using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>"/feel help" shows the in-game guide (the plugin's Guide.IsAsked), so "help" alone must never be a phrase.</summary>
public class GuideWordTests
{
    [Fact]
    public void HelpIsNotAPhrase() =>
        Assert.False(new PhraseMatcher(PhraseBank.LoadEmbedded()).IsPhrase("help"));
}
