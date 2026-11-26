using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>
/// "/feel ?" opens the help window (the plugin's Guide.IsAsked), so "?" must never be a phrase; "/feel help" is an
/// emote like any text.
/// </summary>
public class GuideWordTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly PhraseMatcher Matcher = new(Bank);

    [Fact]
    public void QuestionMarkIsNotAPhrase() => Assert.False(Matcher.IsPhrase("?"));

    [Fact]
    public void HelpPlaysPanic()
    {
        var all = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
            .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
            .ToList();
        var result = new IntentResolver(Matcher).Resolve("help", all);
        Assert.Equal("/panic", Assert.IsType<IntentResult.Play>(result).Emote.Command);
    }
}
