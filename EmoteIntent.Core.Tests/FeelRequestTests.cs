using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

public class FeelRequestTests
{
    private static readonly EmoteInfo Hug = new(1, "Hug", "/hug", [], "General");

    [Theory]
    [InlineData("hug motion", "hug", ChatLog.Motion)]
    [InlineData("so happy to see you MOTION", "so happy to see you", ChatLog.Motion)]
    [InlineData("hug motion!", "hug", ChatLog.Motion)]
    [InlineData("thanks everyone text", "thanks everyone", ChatLog.Text)]
    [InlineData("  give me a hug  ", "give me a hug", ChatLog.GameDefault)]
    public void SplitsATrailingKeyword(string args, string text, ChatLog log) =>
        Assert.Equal(new FeelRequest(text, log), FeelRequest.Parse(args, _ => false));

    [Theory]
    [InlineData("motion")]
    [InlineData("text")]
    public void AKeywordAloneIsWhatThePlayerMeans(string args) =>
        Assert.Equal(new FeelRequest(args, ChatLog.GameDefault), FeelRequest.Parse(args, _ => false));

    [Fact]
    public void APhraseEndingInAKeywordStaysWhole() =>
        Assert.Equal(
            new FeelRequest("reading the quest text", ChatLog.GameDefault),
            FeelRequest.Parse("reading the quest text", text => text == "reading the quest text"));

    [Theory]
    [InlineData(ChatLog.Motion, "/hug motion")]
    [InlineData(ChatLog.Text, "/hug")]
    [InlineData(ChatLog.GameDefault, "/hug")]
    public void BuildsTheCommand(ChatLog log, string command) =>
        Assert.Equal(command, new FeelRequest("hug", log).CommandFor(Hug));
}
