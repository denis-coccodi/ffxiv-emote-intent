namespace EmoteIntent;

/// <summary>
/// How to use /feel, in a few lines: printed in chat by "/feel" alone or "/feel help", and shown in the settings
/// window. The rules players can't guess (bundles, motion/text, typos) belong here.
/// </summary>
internal static class Guide
{
    /// <summary>What "/feel help" asks for; "/feel" alone shows the same.</summary>
    public const string HelpWord = "help";

    /// <summary>One line per tip, in the order they're shown.</summary>
    public static readonly IReadOnlyList<string> Lines =
    [
        "/feel <what you mean> plays the emote that fits: /feel so happy to see you, /feel o7, /feel lmao, /feel hug.",
        "When several fit, they're listed as links: click the one you want. /feelconfig changes when it asks.",
        "/feel all eat, /feel all jumps, /feel all dances: every emote of a bundle you have. /feel all lists the bundles.",
        "End with \"motion\" to play without the emote's chat line (/feel wave motion), or \"text\" to show it.",
        "Typos are fixed (\"exicted\" is read as \"excited\"). Naming an emote you don't have lists the closest ones you do.",
    ];
}
