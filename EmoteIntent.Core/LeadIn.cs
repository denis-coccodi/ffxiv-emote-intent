namespace EmoteIntent.Core;

/// <summary>
/// The words players put in front of what they mean ("feel like being spooky", "i'm feeling so happy", "i want to
/// dance"), which say nothing about the emote. Stripped off, the rest ("spooky", "happy", "dance") can match a phrase.
/// </summary>
public static class LeadIn
{
    // Compact form (lowercase, no apostrophes). Longer ones first, so "i feel like" goes before "i feel".
    private static readonly string[] Words =
    [
        "i would like to", "i am feeling", "i feel like", "feeling like", "id like to", "im feeling", "im going to",
        "i want to", "i need to", "kind of", "going to", "feel like", "time to", "a little", "i wanna", "want to",
        "let me", "i feel", "a bit", "i am", "feeling", "totally", "really", "pretty", "super", "kinda", "gonna",
        "wanna", "being", "feel", "just", "very", "lets", "im", "so", "be",
    ];

    /// <summary>
    /// <paramref name="text"/> in compact form without the lead-in words at its start, as many as there are
    /// ("feel like being spooky" → "spooky"); the compact text itself when nothing would be left.
    /// </summary>
    public static string Strip(string text)
    {
        var compact = TextNormalizer.Compact(text);
        var rest = compact;
        var stripped = true;
        while (stripped)
        {
            stripped = false;
            foreach (var lead in Words)
            {
                if (rest.StartsWith(lead + " ", StringComparison.Ordinal))
                {
                    rest = rest[(lead.Length + 1)..];
                    stripped = true;
                    break;
                }
            }
        }

        return rest.Length > 0 ? rest : compact;
    }
}
