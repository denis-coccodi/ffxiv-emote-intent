using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>The "show the emote's line in the chat log" setting, and the keywords that override it.</summary>
public class ChatLineDefaultTests
{
    [Theory]
    [InlineData(ChatLog.GameDefault, true, ChatLog.GameDefault)]
    [InlineData(ChatLog.GameDefault, false, ChatLog.Motion)]
    [InlineData(ChatLog.Text, false, ChatLog.Text)]
    [InlineData(ChatLog.Motion, true, ChatLog.Motion)]
    public void TheKeywordWinsOverTheSetting(ChatLog typed, bool showChatLine, ChatLog played) =>
        Assert.Equal(played, new FeelRequest("hug", typed).WithDefault(showChatLine).Log);
}
