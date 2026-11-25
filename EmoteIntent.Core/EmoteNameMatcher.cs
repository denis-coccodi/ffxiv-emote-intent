namespace EmoteIntent.Core;

/// <summary>
/// Finds the emote a player named outright: its name ("hug", "Hug"), its command ("/hug") or an alias.
/// The first step before matching by meaning; it never guesses.
/// </summary>
public static class EmoteNameMatcher
{
    /// <summary>The emote whose name, command or alias is exactly <paramref name="text"/>, ignoring case, spaces and a leading slash; null if none.</summary>
    public static EmoteInfo? Match(string text, IEnumerable<EmoteInfo> emotes)
    {
        var wanted = Normalize(text);
        if (wanted.Length == 0)
        {
            return null;
        }

        return emotes.FirstOrDefault(emote =>
            Normalize(emote.Name) == wanted ||
            Normalize(emote.Command) == wanted ||
            emote.Aliases.Any(alias => Normalize(alias) == wanted));
    }

    private static string Normalize(string text) =>
        text.Trim().TrimStart('/').Replace(" ", string.Empty).ToLowerInvariant();
}
