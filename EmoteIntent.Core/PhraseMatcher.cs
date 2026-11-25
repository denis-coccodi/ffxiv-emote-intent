namespace EmoteIntent.Core;

/// <summary>An emote command that fits what the player typed, how well (0 to 1), and the phrase that fit best.</summary>
public sealed record PhraseMatch(string Command, double Score, string Phrase);

/// <summary>
/// Ranks emotes by how close what the player typed is to their phrases: the words both share, rare words counting
/// more than common ones ("hug" says more than "you"). An exact phrase scores 1. Runs on the player's PC, no AI.
/// </summary>
public sealed class PhraseMatcher
{
    /// <summary>The share of a score that depends on word order (see Score).</summary>
    private const double OrderWeight = 0.2;

    private readonly List<Entry> entries = [];
    private readonly Dictionary<string, double> rarity = new(StringComparer.Ordinal);
    private readonly double unknownRarity;

    /// <summary>Prepares the bank's phrases for matching.</summary>
    public PhraseMatcher(PhraseBank bank)
    {
        foreach (var (command, phrases) in bank.Phrases)
        {
            foreach (var phrase in phrases)
            {
                var words = TextNormalizer.Words(phrase);
                entries.Add(new Entry(
                    command, phrase, TextNormalizer.Compact(phrase), words.ToHashSet(StringComparer.Ordinal), Pairs(words)));
            }
        }

        var counts = entries.SelectMany(entry => entry.Words)
            .GroupBy(word => word, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        foreach (var (word, count) in counts)
        {
            rarity[word] = Math.Log(1 + ((double)entries.Count / count));
        }

        unknownRarity = Math.Log(1 + entries.Count);
    }

    /// <summary>
    /// The best-fitting emotes among <paramref name="commands"/> (the ones the character can play), best first,
    /// one entry per command, at most <paramref name="count"/>.
    /// </summary>
    public IReadOnlyList<PhraseMatch> Rank(string text, IEnumerable<string> commands, int count = 5)
    {
        var allowed = commands.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var compact = TextNormalizer.Compact(text);
        var ordered = TextNormalizer.Words(text);
        var words = ordered.ToHashSet(StringComparer.Ordinal);
        if (words.Count == 0)
        {
            return [];
        }

        var wordsWeight = words.Sum(Rarity);
        var pairs = Pairs(ordered);
        return entries
            .Where(entry => allowed.Contains(entry.Command))
            .Select(entry => new PhraseMatch(entry.Command, Score(entry, compact, words, wordsWeight, pairs), entry.Phrase))
            .Where(match => match.Score > 0)
            .GroupBy(match => match.Command, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.MaxBy(match => match.Score)!)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Command, StringComparer.Ordinal)
            .Take(count)
            .ToList();
    }

    // Weighted Dice overlap (twice the rarity of the shared words over the rarity of both sides' words), then up
    // to OrderWeight off when the phrase's consecutive word pairs aren't in the text: "good night all" is closer
    // to "night all" than to "all good", though both share two words with it.
    private double Score(Entry entry, string compact, HashSet<string> words, double wordsWeight, HashSet<string> pairs)
    {
        if (entry.Compact == compact)
        {
            return 1;
        }

        var shared = words.Where(entry.Words.Contains).Sum(Rarity);
        if (shared == 0)
        {
            return 0;
        }

        var dice = 2 * shared / (wordsWeight + entry.Words.Sum(Rarity));
        var inOrder = entry.Pairs.Count == 0 ? 1 : (double)entry.Pairs.Count(pairs.Contains) / entry.Pairs.Count;
        return dice * (1 - OrderWeight + (OrderWeight * inOrder));
    }

    private double Rarity(string word) => rarity.TryGetValue(word, out var value) ? value : unknownRarity;

    private static HashSet<string> Pairs(IReadOnlyList<string> words) =>
        words.Zip(words.Skip(1), (first, second) => $"{first} {second}").ToHashSet(StringComparer.Ordinal);

    private sealed record Entry(string Command, string Phrase, string Compact, HashSet<string> Words, HashSet<string> Pairs);
}
