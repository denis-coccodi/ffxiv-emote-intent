namespace EmoteIntent.Core;

/// <summary>
/// Words that hold a sentence together but say little about which emote it means ("like", "my", "it", "the"). In a
/// partial match they count for little, and they alone never make one: "like books" is about books, not about the
/// Thumbs Up phrase "like". A phrase typed exactly ("like", "so", "me") still matches in full.
/// </summary>
public static class FunctionWords
{
    /// <summary>How much a function word counts next to a word of its own rarity.</summary>
    public const double Weight = 0.35;

    // Pronouns, articles, prepositions, "be", and fillers. Verbs that carry meaning ("did", "got", "have") stay out.
    private static readonly HashSet<string> Words = new(StringComparer.Ordinal)
    {
        "a", "am", "an", "and", "are", "as", "at", "be", "been", "but", "by", "for", "from", "he", "her", "him", "his",
        "i", "if", "im", "in", "is", "it", "its", "ive", "just", "kinda", "like", "me", "my", "of", "on", "or", "our",
        "really", "she", "so", "some", "that", "thats", "the", "their", "them", "then", "there", "they", "this", "to",
        "too", "u", "ur", "us", "very", "was", "we", "were", "with", "you", "youre", "your",
    };

    /// <summary>Whether <paramref name="word"/> (as <see cref="TextNormalizer.Words"/> gives it) is a function word.</summary>
    public static bool Contains(string word) => Words.Contains(word);
}
