using EmoteIntent.Core.Ai;

namespace EmoteIntent.Core.Tests;

/// <summary>How a model's plain-word answer is read: short strings only, at most three, whatever the model says.</summary>
public class MeaningPromptTests
{
    [Theory]
    [InlineData("""{"meanings":["angry","frustrated"]}""", "angry|frustrated")]
    [InlineData("""{"meanings":[" tired ","tired","Tired","sleepy","bored"]}""", "tired|sleepy|bored")]
    [InlineData("""{"meanings":["", 3, null, "cold"]}""", "cold")]
    [InlineData("""{"meanings":["this is a very long answer that is clearly not a plain phrase"]}""", "")]
    [InlineData("""{"emotes":["/hug"]}""", "")]
    [InlineData("nope", "")]
    public void KeepsOnlyShortPlainPhrases(string answer, string expected) =>
        Assert.Equal(expected, string.Join('|', MeaningPrompt.ParseAnswer(answer)));
}
