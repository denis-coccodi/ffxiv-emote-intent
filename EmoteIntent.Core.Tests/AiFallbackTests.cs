using EmoteIntent.Core;
using EmoteIntent.Core.Ai;

namespace EmoteIntent.Core.Tests;

/// <summary>The opt-in AI's Core side: a model's plain words through the phrase bank, and reading Ollama's model list.</summary>
public class AiFallbackTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Fact]
    public void EachMeaningsBestEmoteComesFirstInTheModelsOrder()
    {
        var result = Assert.IsType<IntentResult.Play>(Resolver.ResolveMeanings(["sad", "angry"], AllUnlocked));
        var angry = Assert.IsType<IntentResult.Play>(Resolver.Resolve("angry", AllUnlocked)).Emote;
        Assert.Equal("/slump", result.Emote.Command);
        Assert.Equal(angry.Command, result.Alternatives[0].Command);
    }

    [Fact]
    public void AFirstMeaningTheBankIsUnsureOfOnlySuggests() =>
        Assert.IsType<IntentResult.Suggest>(Resolver.ResolveMeanings(["happy sad", "angry"], AllUnlocked));

    [Fact]
    public void MeaningsTheBankDoesNotKnowFindNothing() =>
        Assert.IsType<IntentResult.NoMatch>(Resolver.ResolveMeanings(["qwzx"], AllUnlocked));

    [Fact]
    public void NoMeaningsFindNothing() => Assert.IsType<IntentResult.NoMatch>(Resolver.ResolveMeanings([], AllUnlocked));

    [Theory]
    [InlineData("gemma3:4b", true)]
    [InlineData("GEMMA3:4B", true)]
    [InlineData("qwen3", true)]
    [InlineData("gemma3", false)]
    [InlineData("granite4:3b", false)]
    public void ReadsOllamasModelList(string model, bool expected) =>
        Assert.Equal(expected, OllamaClient.HasModel(
            """{"models":[{"name":"gemma3:4b"},{"name":"qwen3:latest"}]}""", model));
}
