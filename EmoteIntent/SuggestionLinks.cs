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

    /// <summary>"Pick one: /a (A), /b (B). Click it, or type it." for the "let me pick" setting, even for one emote.</summary>
    public void PrintPick(IReadOnlyList<EmoteInfo> emotes, FeelRequest request) =>
        Print("Pick one: ", emotes, request, ". Click it, or type it.");

    /// <summary>"Also fits: /a (A), /b (B)." after an emote played, each one a link.</summary>
    public void PrintAlsoFits(IReadOnlyList<EmoteInfo> emotes, FeelRequest request) =>
        Print("Also fits: ", emotes, request, ".");

    /// <summary>"Played /cheer (Cheer).": after /feel played an emote by itself, never after a click.</summary>
    public void PrintPlayed(EmoteInfo emote, FeelRequest request) =>
        chatGui.Print($"Played {request.CommandFor(emote)} ({emote.Name}).");

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
