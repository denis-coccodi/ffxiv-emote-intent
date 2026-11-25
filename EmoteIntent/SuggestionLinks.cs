using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using EmoteIntent.Core;

namespace EmoteIntent;

/// <summary>
/// Prints suggested emotes in chat as links: clicking one plays that emote, once, the same as typing its command.
/// A click is the player's own action, so this stays within Dalamud's no-automation rule.
/// </summary>
internal sealed class SuggestionLinks(IChatGui chatGui, Func<uint, EmoteInfo?> unlockedById, Action<EmoteInfo> play) : IDisposable
{
    /// <summary>The UIColor row the links are drawn in, so they stand out from the message text.</summary>
    private const ushort LinkColor = 500;

    private readonly Dictionary<uint, DalamudLinkPayload> links = [];

    /// <summary>"Not sure which you mean: /a (A), /b (B). Click one, or type it." with each emote a link.</summary>
    public void Print(IReadOnlyList<EmoteInfo> emotes)
    {
        var message = new SeStringBuilder().AddText("Not sure which you mean: ");
        for (var i = 0; i < emotes.Count; i++)
        {
            if (i > 0)
            {
                message.AddText(", ");
            }

            message.Add(LinkFor(emotes[i]))
                .AddUiForeground($"{emotes[i].Command} ({emotes[i].Name})", LinkColor)
                .Add(RawPayload.LinkTerminator);
        }

        chatGui.Print(message.AddText(". Click one, or type it.").Build());
    }

    /// <summary>Removes every link handler, so old links in the chat log do nothing once the plugin unloads.</summary>
    public void Dispose() => chatGui.RemoveChatLinkHandler();

    // One handler per emote, keyed by its Emote row id, registered the first time it's suggested.
    private DalamudLinkPayload LinkFor(EmoteInfo emote)
    {
        if (!links.TryGetValue(emote.Id, out var link))
        {
            link = chatGui.AddChatLinkHandler(emote.Id, OnClick);
            links[emote.Id] = link;
        }

        return link;
    }

    // Runs on the game's main thread when the player clicks a link, possibly long after it was printed:
    // check the emote is still unlocked (another character may be logged in by now).
    private void OnClick(uint emoteId, SeString link)
    {
        if (unlockedById(emoteId) is { } emote)
        {
            play(emote);
        }
        else
        {
            chatGui.Print("That emote isn't unlocked on this character.");
        }
    }
}
