namespace EmoteIntent.Core;

/// <summary>
/// An emote command that fits what the player typed, how well (0 to 1), the phrase that fit best, and every command
/// its phrase file plays in order of preference (just <paramref name="Command"/>, or a family's or a feeling's list).
/// </summary>
public sealed record PhraseMatch(string Command, double Score, string Phrase, IReadOnlyList<string> Commands);

/// <summary>
/// Ranks emotes by how close what the player typed is to their phrases: the words both share, rare words counting
/// more than common ones ("hug" says more than "you"). An exact phrase scores 1. Runs on the player's PC, no AI.
/// </summary>
public sealed class PhraseMatcher
{
    /// <summary>The share of a score that depends on word order (see Score).</summary>
    private const double OrderWeight = 0.2;

    /// <summary>The best a phrase that isn't typed exactly can score, so an exact phrase always wins.</summary>
    private const double MaxInexactScore = 0.95;

    /// <summary>A phrase typed exactly once the lead-in is gone ("feel like being spooky"): just below the text itself.</summary>
    private const double StrippedExactScore = 0.97;

    private readonly List<Entry> entries = [];
    private readonly Dictionary<string, double> rarity = new(StringComparer.Ordinal);
    private readonly double unknownRarity;

    /// <summary>Prepares the bank's phrases for matching.</summary>
    public PhraseMatcher(PhraseBank bank)
    {
        foreach (var (key, phrases) in bank.Phrases)
        {
            foreach (var phrase in phrases)
            {
                var words = TextNormalizer.Words(phrase);
                entries.Add(new Entry(
                    key, bank.Commands[key], phrase, Exact(phrase), words.ToHashSet(StringComparer.Ordinal), Pairs(words)));
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

    /// <summary>Whether <paramref name="text"/> is exactly one of the bank's phrases (case and punctuation aside).</summary>
    public bool IsPhrase(string text)
    {
        var exact = Exact(text);
        return entries.Exists(entry => entry.Compact == exact);
    }

    /// <summary>
    /// The best-fitting emotes among <paramref name="commands"/> (the ones the character can play), best first,
    /// one entry per phrase file, at most <paramref name="count"/>. For a family, the command is its first member the
    /// character can play.
    /// </summary>
    public IReadOnlyList<PhraseMatch> Rank(string text, IEnumerable<string> commands, int count = 5)
    {
        var allowed = commands.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The text as typed, and without its lead-in ("feel like being spooky" → "spooky"); each phrase keeps the
        // better of the two scores, so an exact phrase that starts with a lead-in ("so happy to see you") still wins.
        var stripped = LeadIn.Strip(text);
        var queries = new List<Query> { ToQuery(text, isStripped: false) };
        if (stripped != TextNormalizer.Compact(text))
        {
            queries.Add(ToQuery(stripped, isStripped: true));
        }

        queries.RemoveAll(query => query.Words.Count == 0);
        if (queries.Count == 0)
        {
            return [];
        }

        return entries
            .Select(entry => (entry, command: entry.Commands.FirstOrDefault(allowed.Contains)))
            .Where(item => item.command is not null)
            .Select(item => (item.entry.Key, Match: new PhraseMatch(
                item.command!, queries.Max(query => Score(item.entry, query)), item.entry.Phrase, item.entry.Commands)))
            .Where(item => item.Match.Score > 0)
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.MaxBy(item => item.Match.Score).Match)
            // An emote reached through two files (its own and a feeling's list) appears once, at its best.
            .GroupBy(match => match.Command, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.MaxBy(match => match.Score)!)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Command, StringComparer.Ordinal)
            .Take(count)
            .ToList();
    }

    // The phrase exactly (1), else a weighted Dice overlap (twice the rarity of the shared words over the rarity of
    // both sides' words) capped below an exact phrase, so "cheers!" is Toast's "cheers" rather than Cheer's "cheer"
    // (same word once the plural goes); then up to OrderWeight off when the phrase's consecutive word pairs aren't
    // in the text: "good night all" is closer to "night all" than to "all good", though both share two words.
    private double Score(Entry entry, Query query)
    {
        if (entry.Compact == query.Exact)
        {
            return query.IsStripped ? StrippedExactScore : 1;
        }

        // Sharing only function words ("like", "my", "it") makes no match: "like books" isn't Thumbs Up's "like".
        var sharedWords = query.Words.Where(entry.Words.Contains).ToList();
        if (sharedWords.TrueForAll(FunctionWords.Contains))
        {
            return 0;
        }

        var shared = sharedWords.Sum(Rarity);

        var dice = 2 * shared / (query.Weight + entry.Words.Sum(Rarity));
        var inOrder = entry.Pairs.Count == 0 ? 1 : (double)entry.Pairs.Count(query.Pairs.Contains) / entry.Pairs.Count;
        return MaxInexactScore * dice * (1 - OrderWeight + (OrderWeight * inOrder));
    }

    // What a query brings to scoring: its exact form, its words, their total rarity and its consecutive word pairs.
    private Query ToQuery(string text, bool isStripped)
    {
        var ordered = TextNormalizer.Words(text);
        var words = ordered.ToHashSet(StringComparer.Ordinal);
        return new Query(Exact(text), words, words.Sum(Rarity), Pairs(ordered), isStripped);
    }

    private sealed record Query(string Exact, HashSet<string> Words, double Weight, HashSet<string> Pairs, bool IsStripped);

    // What counts as typing a phrase exactly: Compact, minus sentence punctuation around it ("cheers!" is "cheers").
    private static string Exact(string text) => TextNormalizer.Compact(text).Trim('.', ',', '!', '?', ';', ' ');

    // How much a word says about the emote: rarer words more, function words ("like", "my") a fifth of that.
    private double Rarity(string word) =>
        (rarity.TryGetValue(word, out var value) ? value : unknownRarity) * (FunctionWords.Contains(word) ? FunctionWords.Weight : 1);

    private static HashSet<string> Pairs(IReadOnlyList<string> words) =>
        words.Zip(words.Skip(1), (first, second) => $"{first} {second}").ToHashSet(StringComparer.Ordinal);

    private sealed record Entry(
        string Key, IReadOnlyList<string> Commands, string Phrase, string Compact, HashSet<string> Words, HashSet<string> Pairs);
}
