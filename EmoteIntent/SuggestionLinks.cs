using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using EmoteIntent.Core;

namespace EmoteIntent;

/// <summary>
/// Prints suggested emotes in chat as links: clicking one plays that emote, once, the same as typing its command.
/// A click is the player's own action, so this stays within Dalamud's no-automation rule. A bundle's link ("all 9
/// eat emotes") lists that bundle's emotes the same way.
/// </summary>
internal sealed class SuggestionLinks(
    IChatGui chatGui, Func<IReadOnlyList<EmoteInfo>> unlocked, EmoteBundles bundles, Action<EmoteInfo, FeelRequest> play)
    : IDisposable
{
    /// <summary>The UIColor row the links are drawn in, so they stand out from the message text.</summary>
    private const ushort LinkColor = 500;

    /// <summary>The link id bit that marks a bundle's link rather than an emote's.</summary>
    private const uint BundleBit = 0x8000_0000;

    private readonly Dictionary<uint, DalamudLinkPayload> links = [];

    /// <summary>
    /// "Not sure which you mean: /a (A), /b (B). Click one, or type it." with each emote a link that plays it the way
    /// the player asked (with "motion", the links play without the chat line too).
    /// </summary>
    public void Print(IReadOnlyList<EmoteInfo> emotes, FeelRequest request) =>
        Print("Not sure which you mean: ", emotes, request, ". Click one, or type it.", emotes[0]);

    /// <summary>"Pick one: /a (A), /b (B). Click it, or type it." for the "let me pick" setting, even for one emote.</summary>
    public void PrintPick(IReadOnlyList<EmoteInfo> emotes, FeelRequest request) =>
        Print("Pick one: ", emotes, request, ". Click it, or type it.", emotes[0]);

    /// <summary>
    /// "You don't have Hug (/hug) unlocked. Closest you have: /a (A), /b (B). Click one, or type it.", or that nothing
    /// you have comes close.
    /// </summary>
    public void PrintLocked(EmoteInfo wanted, IReadOnlyList<EmoteInfo> closest, FeelRequest request)
    {
        var lead = $"You don't have {wanted.Name} ({wanted.Command}) unlocked";
        if (closest.Count == 0)
        {
            Print(lead + ", and none of your emotes comes close.", [], request, string.Empty, wanted);
        }
        else
        {
            Print(lead + ". Closest you have: ", closest, request, ". Click one, or type it.", wanted);
        }
    }

    /// <summary>"Also fits: /a (A), /b (B)." after an emote played, each one a link.</summary>
    public void PrintAlsoFits(EmoteInfo played, IReadOnlyList<EmoteInfo> emotes, FeelRequest request) =>
        Print("Also fits: ", emotes, request, ".", played);

    /// <summary>"Played /cheer (Cheer).": after /feel played an emote by itself, never after a click.</summary>
    public void PrintPlayed(EmoteInfo emote, FeelRequest request) =>
        Print($"Played {request.CommandFor(emote)} ({emote.Name}).", [], request, string.Empty, emote);

    /// <summary>"Eat (9): /a (A), /b (B)… Click one, or type it. 2 more aren't unlocked." for "/feel all eat".</summary>
    public void PrintBundle(EmoteBundle bundle, FeelRequest request)
    {
        var emotes = EmoteBundles.UnlockedOf(bundle, unlocked());
        var missing = bundle.Commands.Distinct(StringComparer.OrdinalIgnoreCase).Count() - emotes.Count;
        if (emotes.Count == 0)
        {
            chatGui.Print($"You have none of the {bundle.Name} emotes unlocked.");
            return;
        }

        var tail = missing switch
        {
            0 => ". Click one, or type it.",
            1 => ". Click one, or type it. 1 more isn't unlocked.",
            _ => $". Click one, or type it. {missing} more aren't unlocked.",
        };
        Print($"{bundle.Name} ({emotes.Count}): ", emotes, request, tail, anchor: null);
    }

    /// <summary>"Bundles: Eat (9), Drink (4)… Click one to list its emotes, or type /feel all eat." for "/feel all".</summary>
    public void PrintBundles(FeelRequest request)
    {
        var have = unlocked();
        var message = new SeStringBuilder().AddText("Bundles: ");
        var first = true;
        foreach (var bundle in bundles.All)
        {
            var count = EmoteBundles.UnlockedOf(bundle, have).Count;
            if (count == 0)
            {
                continue;
            }

            message.AddText(first ? string.Empty : ", ");
            AddLink(message, BundleLinkFor(bundle, request), $"{bundle.Name} ({count})");
            first = false;
        }

        chatGui.Print(message.AddText(". Click one to list its emotes, or type /feel all eat.").Build());
    }

    private void Print(string lead, IReadOnlyList<EmoteInfo> emotes, FeelRequest request, string tail, EmoteInfo? anchor)
    {
        var message = new SeStringBuilder().AddText(lead);
        for (var i = 0; i < emotes.Count; i++)
        {
            message.AddText(i > 0 ? ", " : string.Empty);
            AddLink(message, LinkFor(emotes[i], request.Log == ChatLog.Motion), $"{request.CommandFor(emotes[i])} ({emotes[i].Name})");
        }

        message.AddText(tail);
        AddMore(message, anchor, emotes, request);
        chatGui.Print(message.Build());
    }

    // " See all 9 eat emotes." when the anchor (the emote played, wanted or first suggested) or a listed emote is in a
    // bundle with unlocked emotes the line doesn't show.
    private void AddMore(SeStringBuilder message, EmoteInfo? anchor, IReadOnlyList<EmoteInfo> listed, FeelRequest request)
    {
        var candidates = anchor is null ? listed : listed.Prepend(anchor);
        if (anchor is null || candidates.Select(emote => bundles.Of(emote.Command)).FirstOrDefault(bundle => bundle is not null) is not { } bundle)
        {
            return;
        }

        var members = EmoteBundles.UnlockedOf(bundle, unlocked());
        var shown = listed.Append(anchor).Select(emote => emote.Command).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (members.Count < 2 || members.All(emote => shown.Contains(emote.Command)))
        {
            return;
        }

        message.AddText(" See ");
        AddLink(message, BundleLinkFor(bundle, request), $"all {members.Count} {bundle.Name.ToLowerInvariant()} emotes");
        message.AddText(".");
    }

    private static void AddLink(SeStringBuilder message, DalamudLinkPayload link, string text) =>
        message.Add(link).AddUiForeground(text, LinkColor).Add(RawPayload.LinkTerminator);

    /// <summary>Removes every link handler, so old links in the chat log do nothing once the plugin unloads.</summary>
    public void Dispose() => chatGui.RemoveChatLinkHandler();

    // One handler per emote and way of playing it, registered the first time it's suggested. The link id is the
    // Emote row id shifted left once, its lowest bit saying "motion" (Emote ids are far below 2^30).
    private DalamudLinkPayload LinkFor(EmoteInfo emote, bool motion) => Link((emote.Id << 1) | (motion ? 1u : 0u));

    // A bundle's link: its place in the bundle list, the same way, with the bundle bit set.
    private DalamudLinkPayload BundleLinkFor(EmoteBundle bundle, FeelRequest request) =>
        Link(BundleBit | ((uint)IndexOf(bundle) << 1) | (request.Log == ChatLog.Motion ? 1u : 0u));

    private int IndexOf(EmoteBundle bundle)
    {
        for (var i = 0; i < bundles.All.Count; i++)
        {
            if (ReferenceEquals(bundles.All[i], bundle))
            {
                return i;
            }
        }

        throw new ArgumentException($"Unknown bundle {bundle.Name}", nameof(bundle));
    }

    private DalamudLinkPayload Link(uint id)
    {
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
        if ((id & BundleBit) != 0)
        {
            var bundle = bundles.All[(int)((id & ~BundleBit) >> 1)];
            PrintBundle(bundle, new FeelRequest(bundle.Name, log));
        }
        else if (unlocked().FirstOrDefault(emote => emote.Id == id >> 1) is { } emote)
        {
            play(emote, new FeelRequest(emote.Name, log));
        }
        else
        {
            chatGui.Print("That emote isn't unlocked on this character.");
        }
    }
}
