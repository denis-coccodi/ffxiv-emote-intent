using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using EmoteIntent.Core;

namespace EmoteIntent;

/// <summary>
/// Prints suggested emotes in chat as links: clicking one plays that emote, once, the same as typing its command.
/// A click is the player's own action, so this stays within Dalamud's no-automation rule.
/// </summary>
internal sealed class SuggestionLinks(
    IChatGui chatGui, Func<uint, EmoteInfo?> unlockedById, Action<EmoteInfo, FeelRequest> play) : IDisposable
{
    /// <summary>The UIColor row the links are drawn in, so they stand out from the message text.</summary>
    private const ushort LinkColor = 500;

    private readonly Dictionary<uint, DalamudLinkPayload> links = [];

    /// <summary>
    /// "Not sure which you mean: /a (A), /b (B). Click one, or type it." with each emote a link that plays it the way
    /// the player asked (with "motion", the links play without the chat line too).
    /// </summary>
    public void Print(IReadOnlyList<EmoteInfo> emotes, FeelRequest request) =>
        Print("Not sure which you mean: ", emotes, request, ". Click one, or type it.");

    /// <summary>"Also fits: /a (A), /b (B)." after an emote played, each one a link, for other emotes that fit well.</summary>
    public void PrintAlsoFits(IReadOnlyList<EmoteInfo> emotes, FeelRequest request) =>
        Print("Also fits: ", emotes, request, ".");

    /// <summary>
    /// "Playing /read (Read). Others: /a (A), /b (B)." before an emote plays, for the "always show" setting; just
    /// "Playing /read (Read)." when nothing else fits.
    /// </summary>
    public void PrintPlaying(EmoteInfo emote, IReadOnlyList<EmoteInfo> others, FeelRequest request)
    {
        var playing = $"Playing {request.CommandFor(emote)} ({emote.Name})";
        if (others.Count == 0)
        {
            chatGui.Print(playing + ".");
        }
        else
        {
            Print(playing + ". Others: ", others, request, ".");
        }
    }

    private void Print(string lead, IReadOnlyList<EmoteInfo> emotes, FeelRequest request, string tail)
    {
        var message = new SeStringBuilder().AddText(lead);
        for (var i = 0; i < emotes.Count; i++)
        {
            if (i > 0)
            {
                message.AddText(", ");
            }

            message.Add(LinkFor(emotes[i], request.Log == ChatLog.Motion))
                .AddUiForeground($"{request.CommandFor(emotes[i])} ({emotes[i].Name})", LinkColor)
                .Add(RawPayload.LinkTerminator);
        }

        chatGui.Print(message.AddText(tail).Build());
    }

    /// <summary>Removes every link handler, so old links in the chat log do nothing once the plugin unloads.</summary>
    public void Dispose() => chatGui.RemoveChatLinkHandler();

    // One handler per emote and way of playing it, registered the first time it's suggested. The link id is the
    // Emote row id shifted left once, its lowest bit saying "motion" (Emote ids are far below 2^31).
    private DalamudLinkPayload LinkFor(EmoteInfo emote, bool motion)
    {
        var id = (emote.Id << 1) | (motion ? 1u : 0u);
        if (!links.TryGetValue(id, out var link))
        {
            link = chatGui.AddChatLinkHandler(id, OnClick);
            links[id] = link;
        }

        return link;
    }

    // Runs on the game's main thread when the player clicks a link, possibly long after it was printed:
    // check the emote is still unlocked (another character may be logged in by now).
    private void OnClick(uint id, SeString link)
    {
        var log = (id & 1) == 1 ? ChatLog.Motion : ChatLog.GameDefault;
        if (unlockedById(id >> 1) is { } emote)
        {
            play(emote, new FeelRequest(emote.Name, log));
        }
        else
        {
            chatGui.Print("That emote isn't unlocked on this character.");
        }
    }
}
