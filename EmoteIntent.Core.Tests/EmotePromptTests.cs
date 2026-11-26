using EmoteIntent.Core;
using EmoteIntent.Core.Ai;

namespace EmoteIntent.Core.Tests;

/// <summary>The AI request and how its answer is read: only real, unlocked emotes ever come back.</summary>
public class EmotePromptTests
{
    private static readonly string[] Unlocked = ["/hug", "/cry", "/panic"];

    [Fact]
    public void TheSystemMessageHoldsEveryEmotesGuideLine()
    {
        var guide = EmoteGuide.LoadEmbedded();
        var prompt = new EmotePrompt(guide);

        Assert.All(guide.Entries.Keys, command => Assert.Contains($"\n{command} | ", prompt.SystemMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void TheSchemaOnlyAllowsTheGivenCommands()
    {
        var schema = EmotePrompt.Schema(Unlocked).ToJsonString();

        Assert.Contains("\"enum\":[\"/hug\",\"/cry\",\"/panic\"]", schema, StringComparison.Ordinal);
        Assert.Contains($"\"maxItems\":{EmotePrompt.MaxEmotes}", schema, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"emotes":["/panic","/cry"]}""", "/panic /cry")]
    [InlineData("""{"emotes":["panic"]}""", "/panic")]
    [InlineData("""{"emotes":["/dance","/hug","/hug"]}""", "/hug")]
    [InlineData("""{"emotes":["/hug","/cry","/panic","/hug"]}""", "/hug /cry /panic")]
    [InlineData("""{"emotes":[1, null, "/cry"]}""", "/cry")]
    [InlineData("""{"other":["/hug"]}""", "")]
    [InlineData("not json", "")]
    public void KeepsOnlyUnlockedCommandsFromTheAnswer(string answer, string expected) =>
        Assert.Equal(expected, string.Join(' ', EmotePrompt.ParseAnswer(answer, Unlocked)));
}
