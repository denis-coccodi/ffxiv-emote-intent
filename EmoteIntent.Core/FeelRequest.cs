namespace EmoteIntent.Core;

/// <summary>Whether a played emote also writes its line in the chat log ("You hug Avar.").</summary>
public enum ChatLog
{
    /// <summary>The game's own behaviour: the plain command, which shows the line.</summary>
    GameDefault,

    /// <summary>The animation only, no chat line: the game's "/hug motion".</summary>
    Motion,

    /// <summary>With the chat line, asked for explicitly (the plain command).</summary>
    Text,
}

/// <summary>What the player typed after /feel: what they mean, and whether the emote should show in the chat log.</summary>
/// <param name="Text">What they mean ("so happy to see you"), without the trailing keyword.</param>
/// <param name="Log">"motion" or "text" as the last word, else <see cref="ChatLog.GameDefault"/>.</param>
public sealed record FeelRequest(string Text, ChatLog Log)
{
    /// <summary>
    /// Splits a trailing "motion" or "text" off what the player typed. It only counts as the last word after other
    /// words, and not when the whole text is a phrase of the bank ("reading the quest text").
    /// </summary>
    public static FeelRequest Parse(string args, Func<string, bool> isPhrase)
    {
        var text = args.Trim();
        var cut = text.LastIndexOf(' ');
        if (cut < 0 || isPhrase(text))
        {
            return new FeelRequest(text, ChatLog.GameDefault);
        }

        var log = text[(cut + 1)..].Trim('.', '!', '?', ',').ToLowerInvariant() switch
        {
            "motion" => ChatLog.Motion,
            "text" => ChatLog.Text,
            _ => ChatLog.GameDefault,
        };
        return log == ChatLog.GameDefault ? new FeelRequest(text, log) : new FeelRequest(text[..cut].TrimEnd(), log);
    }

    /// <summary>The chat command that plays <paramref name="emote"/> this way ("/hug" or "/hug motion").</summary>
    public string CommandFor(EmoteInfo emote) => Log == ChatLog.Motion ? $"{emote.Command} motion" : emote.Command;

    /// <summary>
    /// This request with the player's default applied: without a trailing "motion" or "text", the emote's chat line
    /// shows only when <paramref name="showChatLine"/> is on. "motion" and "text" always win for that one emote.
    /// </summary>
    public FeelRequest WithDefault(bool showChatLine) =>
        Log == ChatLog.GameDefault && !showChatLine ? this with { Log = ChatLog.Motion } : this;
}
