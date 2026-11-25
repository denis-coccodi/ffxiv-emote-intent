using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("I'm  SO happy", "im so happy")]
    [InlineData("  o/  ", "o/")]
    [InlineData("That’s funny", "thats funny")]
    public void CompactLowercasesDropsApostrophesAndKeepsSymbols(string text, string expected) =>
        Assert.Equal(expected, TextNormalizer.Compact(text));

    [Fact]
    public void WordsSplitOnPunctuationAndDropPlurals() =>
        Assert.Equal(["hug", "for", "everyone"], TextNormalizer.Words("Hugs for everyone!!"));

    [Fact]
    public void WordsKeepEmoticonsWhole() =>
        Assert.Equal(["love", "you", "<3", ":d", "o/", "10/10"], TextNormalizer.Words("love you <3 :D o/ 10/10"));

    [Fact]
    public void WordsIgnoreSentencePunctuation() =>
        Assert.Equal(["hi", "there", "lol"], TextNormalizer.Words("hi! there... lol?"));

    [Fact]
    public void WordsKeepShortWordsAndDoubleS() =>
        Assert.Equal(["yes", "kiss", "was"], TextNormalizer.Words("yes kiss was"));
}
